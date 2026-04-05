using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Blog.Api.Common.HealthChecks;

public class DiskSpaceHealthCheck(IWebHostEnvironment env) : IHealthCheck
{
    private const long MinimumFreeBytes = 512 * 1024 * 1024; // 512 MB

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var rootPath = Path.GetPathRoot(env.ContentRootPath) ?? env.ContentRootPath;
        var driveInfo = new DriveInfo(rootPath);

        if (driveInfo.AvailableFreeSpace < MinimumFreeBytes)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Low disk space: {driveInfo.AvailableFreeSpace / (1024 * 1024)} MB available."));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Disk space OK: {driveInfo.AvailableFreeSpace / (1024 * 1024)} MB available."));
    }
}
