namespace Blog.Api.Services;

/// <summary>
/// Tracks per-email login attempt rate limits.
/// Enforces a maximum of 5 login attempts per normalized email address within any 15-minute window.
/// </summary>
public interface IEmailRateLimitService
{
    /// <summary>
    /// Records a login attempt for the given email and returns whether the attempt is permitted.
    /// Returns <c>true</c> when the attempt is within the allowed quota.
    /// Returns <c>false</c> when the email has exceeded the allowed attempt count; in that case
    /// <paramref name="retryAfterSeconds"/> is set to the number of whole seconds until the oldest
    /// attempt slides out of the window and the quota is restored.
    /// </summary>
    bool TryAcquire(string email, out int retryAfterSeconds);
}
