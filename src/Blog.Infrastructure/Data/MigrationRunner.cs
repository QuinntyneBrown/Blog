using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Blog.Infrastructure.Data;

public class MigrationRunner(BlogDbContext context, ILogger<MigrationRunner> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var pending = await context.Database.GetPendingMigrationsAsync(cancellationToken);
        var pendingList = pending.ToList();

        if (pendingList.Count == 0)
        {
            logger.LogInformation("No pending database migrations.");
            return;
        }

        logger.LogInformation("Applying {Count} pending migration(s)...", pendingList.Count);
        foreach (var migration in pendingList)
            logger.LogInformation("  → {Migration}", migration);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await context.Database.MigrateAsync(cancellationToken);
        sw.Stop();

        logger.LogInformation("Migrations applied successfully in {Elapsed}ms.", sw.ElapsedMilliseconds);
    }
}
