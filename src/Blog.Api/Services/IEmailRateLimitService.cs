namespace Blog.Api.Services;

/// <summary>
/// Tracks per-email login attempt rate limits.
/// Enforces a maximum of 5 login attempts per normalized email address within any 15-minute window.
/// </summary>
public interface IEmailRateLimitService
{
    /// <summary>
    /// Records a login attempt for the given email and returns whether the attempt is permitted.
    /// Returns false when the email has exceeded the allowed attempt count within the window.
    /// </summary>
    bool TryAcquire(string email);
}
