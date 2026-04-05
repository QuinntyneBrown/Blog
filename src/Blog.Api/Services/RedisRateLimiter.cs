using StackExchange.Redis;
using System.Threading.RateLimiting;

namespace Blog.Api.Services;

/// <summary>
/// A sliding-window rate limiter backed by Redis sorted sets.
/// Each partition key maps to a Redis sorted set where scores are Unix timestamps.
/// Expired entries are pruned on each Acquire call.
/// </summary>
public sealed class RedisRateLimiter : RateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _keyPrefix;
    private readonly int _permitLimit;
    private readonly TimeSpan _window;

    public RedisRateLimiter(IConnectionMultiplexer redis, string keyPrefix, int permitLimit, TimeSpan window)
    {
        _redis = redis;
        _keyPrefix = keyPrefix;
        _permitLimit = permitLimit;
        _window = window;
    }

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        return new ValueTask<RateLimitLease>(AttemptAcquireCore(permitCount));
    }

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{_keyPrefix}";
            var now = DateTimeOffset.UtcNow;
            var windowStart = now.Add(-_window);

            // Remove entries outside the window
            db.SortedSetRemoveRangeByScore(key, double.NegativeInfinity, windowStart.ToUnixTimeMilliseconds());

            var currentCount = db.SortedSetLength(key);

            if (currentCount >= _permitLimit)
            {
                // Get the oldest entry to calculate retry-after
                var oldest = db.SortedSetRangeByRankWithScores(key, 0, 0);
                TimeSpan retryAfter = _window;
                if (oldest.Length > 0)
                {
                    var oldestTime = DateTimeOffset.FromUnixTimeMilliseconds((long)oldest[0].Score);
                    retryAfter = oldestTime.Add(_window) - now;
                    if (retryAfter < TimeSpan.FromSeconds(1))
                        retryAfter = TimeSpan.FromSeconds(1);
                }
                return new RejectedLease(retryAfter);
            }

            // Add current request
            var member = $"{now.ToUnixTimeMilliseconds()}:{Guid.NewGuid():N}";
            db.SortedSetAdd(key, member, now.ToUnixTimeMilliseconds());
            db.KeyExpire(key, _window.Add(TimeSpan.FromMinutes(1)));

            return new AcceptedLease();
        }
        catch
        {
            // If Redis is unavailable, permit the request (fail-open)
            return new AcceptedLease();
        }
    }

    private sealed class AcceptedLease : RateLimitLease
    {
        public override bool IsAcquired => true;
        public override IEnumerable<string> MetadataNames => [];
        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }

    private sealed class RejectedLease(TimeSpan retryAfter) : RateLimitLease
    {
        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames => [MetadataName.RetryAfter.Name];
        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (metadataName == MetadataName.RetryAfter.Name)
            {
                metadata = retryAfter;
                return true;
            }
            metadata = null;
            return false;
        }
    }
}
