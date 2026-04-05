using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Blog.Infrastructure.Data;

public class SeedData(IUnitOfWork uow, ILogger<SeedData> logger, IConfiguration configuration)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var seedSection = configuration.GetSection("Seed:AdminUser");
        var email = seedSection["Email"];
        var displayName = seedSection["DisplayName"] ?? "Quinn Brown";
        var passwordHash = seedSection["PasswordHash"];

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(passwordHash))
        {
            logger.LogDebug("No admin seed user configured — skipping admin user seed.");
            return;
        }

        var existing = await uow.Users.GetByEmailAsync(email, cancellationToken);
        if (existing != null)
        {
            logger.LogDebug("Admin user {Email} already exists — skipping seed.", email);
            return;
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow
        };

        await uow.Users.AddAsync(user, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Admin user {Email} seeded successfully.", email);
    }
}
