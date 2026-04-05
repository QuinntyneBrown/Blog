using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Blog.Infrastructure.Data;

/// <summary>
/// Hosted service that applies any pending EF Core migrations before the application
/// begins serving requests.
/// Design reference: docs/detailed-designs/10-data-persistence/README.md, Section 3.5
/// Open Question 4 resolved: IHostedService on startup.
/// </summary>
public class MigrationRunner(IServiceScopeFactory scopeFactory, ILogger<MigrationRunner> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var pending = await context.Database.GetPendingMigrationsAsync(cancellationToken);
        var pendingList = pending.ToList();

        if (pendingList.Count == 0)
        {
            logger.LogInformation("No pending database migrations.");
            return;
        }

        logger.LogInformation("Applying {Count} pending migration(s)...", pendingList.Count);
        foreach (var migration in pendingList)
            logger.LogInformation("  -> {Migration}", migration);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to apply database migrations. Application will terminate.");
            throw;
        }

        sw.Stop();
        logger.LogInformation("Migrations applied successfully in {ElapsedMs}ms.", sw.ElapsedMilliseconds);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
