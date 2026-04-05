using StackExchange.Redis;

namespace Blog.Api.Services;

/// <summary>
/// Redis-backed email rate limiter that persists counters across restarts and
/// synchronizes across multiple application instances.
/// Uses a Redis sorted set per email with Unix-ms timestamps as scores.
/// Falls back to in-memory <see cref="EmailRateLimitService"/> when Redis is unavailable.
/// Design reference: docs/detailed-designs/08-security-hardening/README.md, Section 3.2.
/// </summary>
public sealed class RedisEmailRateLimitService : IEmailRateLimitService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly int _maxAttempts;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private const string KeyPrefix = "ratelimit:email-login:";

    // In-memory fallback for when Redis operations fail
    private readonly EmailRateLimitService _fallback;

    public RedisEmailRateLimitService(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        _redis = redis;
        _maxAttempts = configuration.GetValue("RateLimiting:EmailLoginMaxAttempts", 5);
        _fallback = new EmailRateLimitService(configuration);
    }

    public bool TryAcquire(string email, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}{email.Trim().ToLowerInvariant()}";
            var now = DateTimeOffset.UtcNow;
            var windowStart = now.Add(-Window);

            // Remove entries outside the window
            db.SortedSetRemoveRangeByScore(key, double.NegativeInfinity, windowStart.ToUnixTimeMilliseconds());

            var currentCount = db.SortedSetLength(key);

            if (currentCount >= _maxAttempts)
            {
                var oldest = db.SortedSetRangeByRankWithScores(key, 0, 0);
                if (oldest.Length > 0)
                {
                    var oldestTime = DateTimeOffset.FromUnixTimeMilliseconds((long)oldest[0].Score);
                    var windowExpiry = oldestTime.Add(Window);
                    retryAfterSeconds = (int)Math.Ceiling((windowExpiry - now).TotalSeconds);
                    if (retryAfterSeconds < 1) retryAfterSeconds = 1;
                }
                else
                {
                    retryAfterSeconds = (int)Window.TotalSeconds;
                }
                return false;
            }

            // Add current attempt
            var member = $"{now.ToUnixTimeMilliseconds()}:{Guid.NewGuid():N}";
            db.SortedSetAdd(key, member, now.ToUnixTimeMilliseconds());
            db.KeyExpire(key, Window.Add(TimeSpan.FromMinutes(1)));

            return true;
        }
        catch
        {
            // Fall back to in-memory when Redis is unavailable
            return _fallback.TryAcquire(email, out retryAfterSeconds);
        }
    }
}
