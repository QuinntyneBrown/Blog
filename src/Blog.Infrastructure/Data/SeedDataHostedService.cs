using Blog.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Blog.Infrastructure.Data;

/// <summary>
/// Hosted service that seeds required reference data after migrations have been applied.
/// Registered after MigrationRunner so it executes once the schema is current.
/// </summary>
public class SeedDataHostedService(IServiceScopeFactory scopeFactory, IHostEnvironment env) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedData>>();
        var seedData = new SeedData(uow, logger, configuration);
        await seedData.SeedAsync(cancellationToken);

        if (env.IsDevelopment())
            await seedData.SeedDevelopmentDataAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
