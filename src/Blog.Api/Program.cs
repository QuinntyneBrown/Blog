using System.Text;
using Blog.Api.Common.Behaviors;
using Blog.Api.Middleware;
using Blog.Api.Services;
using Blog.Domain.Interfaces;
using Blog.Infrastructure.Data;
using Blog.Infrastructure.Data.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Kestrel request size limits (Design 06, OQ-4: 1 MB default, 10 MB for file uploads).
// The digital-asset upload endpoint overrides this to 10 MB via [RequestSizeLimit] /
// [RequestFormLimits] attributes on the action.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1 * 1024 * 1024; // 1 MB default
});

// Serilog
builder.Host.UseSerilog((context, services, config) =>
    config.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(services)
        .Enrich.With<Blog.Api.Core.LogSanitizingEnricher>());

// Database
builder.Services.AddDbContextPool<BlogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories & UnitOfWork
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDigitalAssetRepository, DigitalAssetRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddSingleton<ISlugGenerator, SlugGenerator>();
builder.Services.AddSingleton<IMarkdownConverter, MarkdownConverter>();
builder.Services.AddSingleton<IReadingTimeCalculator, ReadingTimeCalculator>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IEmailRateLimitService, EmailRateLimitService>();
builder.Services.AddSingleton<ICacheInvalidator, CacheInvalidator>();
builder.Services.AddSingleton<IETagGenerator, ETagGenerator>();
builder.Services.AddSingleton<IImageVariantGenerator, ImageVariantGenerator>();
builder.Services.AddScoped<ITokenService, TokenService>();

// MediatR + Validation
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSettings["Secret"]!;
if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException("JWT signing key must be at least 256 bits (32 bytes). Check the Jwt:Secret configuration value.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login-ip", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                PermitLimit = 10,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    options.AddPolicy("write-endpoints", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.User?.FindFirst("sub")?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                PermitLimit = 60,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }
        else
        {
            context.HttpContext.Response.Headers.RetryAfter = "60";
        }
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsync(
            "{\"type\":\"https://tools.ietf.org/html/rfc6585#section-4\",\"title\":\"Too Many Requests\",\"status\":429,\"detail\":\"Rate limit exceeded. Please try again later.\"}",
            cancellationToken);
    };
});

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes
        .Concat(["image/svg+xml"]);
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// Response Caching
builder.Services.AddResponseCaching();

// Controllers + Razor Pages
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
})
    .AddMvcOptions(options =>
    {
        // Cache profile for public HTML pages: max-age=60, stale-while-revalidate=600
        // Design reference: docs/detailed-designs/07-web-performance/README.md, Section 3.1
        options.CacheProfiles.Add("HtmlPage", new Microsoft.AspNetCore.Mvc.CacheProfile
        {
            Duration = 60,
            Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.Any,
            VaryByHeader = "Accept-Encoding",
        });
    });

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BlogDbContext>("database")
    .AddCheck<Blog.Api.Common.HealthChecks.DiskSpaceHealthCheck>("diskSpace");

// Database health check timeout (Design 09, Section 3.4: 5 seconds)
builder.Services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
{
    foreach (var reg in options.Registrations)
        if (reg.Name == "database")
            reg.Timeout = TimeSpan.FromSeconds(5);
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — only origins listed under Cors:AllowedOrigins in configuration are permitted.
// See docs/detailed-designs/08-security-hardening/README.md, Section 3.4.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            .SetPreflightMaxAge(TimeSpan.FromSeconds(7200)));
});

// Session (for admin JWT storage)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.Name = ".blog.admin.session";
});

// HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Migration runner and seed data — both registered as hosted services so they execute
// before the Kestrel HTTP pipeline accepts traffic (design 10, Section 3.5, OQ-4).
// MigrationRunner must be registered before SeedDataHostedService to guarantee ordering.
builder.Services.AddHostedService<Blog.Infrastructure.Data.MigrationRunner>();
builder.Services.AddHostedService<Blog.Infrastructure.Data.SeedDataHostedService>();

var app = builder.Build();

// Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ResponseEnvelopeMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<SlugRedirectMiddleware>();
app.UseStaticFiles();

app.UseRouting();
app.UseCors();
app.UseSession();
app.UseRateLimiter();
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString().ToLowerInvariant()
        });
        await context.Response.WriteAsync(result);
    }
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString().ToLowerInvariant())
        });
        await context.Response.WriteAsync(result);
    }
});

await app.RunAsync();
