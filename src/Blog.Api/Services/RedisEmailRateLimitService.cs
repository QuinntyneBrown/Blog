using StackExchange.Redis;

namespace Blog.Api.Services;

/// <summary>
/// Redis-backed sliding-window rate limiter scoped to a normalized email address.
/// Uses a Redis sorted set per email key where each member is a unique attempt ID
/// scored by its UTC timestamp (Unix milliseconds).
/// Enforces a maximum of <c>RateLimiting:EmailLoginMaxAttempts</c> login attempts per email
/// within any 15-minute window (default: 5).
/// Design reference: Feature 08, Section 8, Open Question 2 — Resolved: Redis.
/// </summary>
public sealed class RedisEmailRateLimitService : IEmailRateLimitService
{
    private readonly int _maxAttempts;
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private const string KeyPrefix = "ratelimit:email:";

    public RedisEmailRateLimitService(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        _redis = redis;
        _maxAttempts = configuration.GetValue("RateLimiting:EmailLoginMaxAttempts", 5);
    }

    public bool TryAcquire(string email, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;

        var key = KeyPrefix + email.Trim().ToLowerInvariant();
        var db = _redis.GetDatabase();
        var now = DateTimeOffset.UtcNow;
        var nowScore = now.ToUnixTimeMilliseconds();
        var cutoffScore = now.Add(-Window).ToUnixTimeMilliseconds();

        // Remove entries outside the sliding window
        db.SortedSetRemoveRangeByScore(key, double.NegativeInfinity, cutoffScore, Exclude.None);

        // Count current entries in the window
        var count = db.SortedSetLength(key);

        if (count >= _maxAttempts)
        {
            // The oldest entry in the set determines when the window reopens.
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
                retryAfterSeconds = 1;
            }

            return false;
        }

        // Record this attempt with a unique member (score = timestamp, member = unique id)
        db.SortedSetAdd(key, Guid.NewGuid().ToString("N"), nowScore);

        // Set expiry on the key so Redis auto-cleans old keys
        db.KeyExpire(key, Window.Add(TimeSpan.FromMinutes(1)));

        return true;
    }
}
