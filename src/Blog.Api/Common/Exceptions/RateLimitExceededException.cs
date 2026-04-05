namespace Blog.Api.Common.Exceptions;

/// <summary>
/// Thrown when a rate limit is exceeded. Carries the number of seconds the caller
/// should wait before retrying, for use in the <c>Retry-After</c> response header.
/// </summary>
public class RateLimitExceededException(string message, int retryAfterSeconds = 0) : Exception(message)
{
    /// <summary>
    /// Number of whole seconds until the rate-limit window resets and the caller may retry.
    /// Zero means unknown / not applicable (the <c>Retry-After</c> header will be omitted).
    /// </summary>
    public int RetryAfterSeconds { get; } = retryAfterSeconds;
}
