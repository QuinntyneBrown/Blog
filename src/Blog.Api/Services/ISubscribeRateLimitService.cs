using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Blog.Api.Services;

public interface ISubscribeRateLimitService
{
    bool TryAcquire(string email, out int retryAfterSeconds);
}

/// <summary>
/// In-memory per-email rate limiter for newsletter subscriptions.
/// Enforces 2 requests per 1-hour sliding window per normalized email (design §7).
/// The email is hashed before storage — plaintext never appears in rate limiter storage.
/// </summary>
public sealed class SubscribeRateLimitService : ISubscribeRateLimitService
{
    private const int MaxAttempts = 2;
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _attempts = new(StringComparer.Ordinal);

    public bool TryAcquire(string email, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;

        var key = HashEmail(email);
        var now = DateTime.UtcNow;
        var cutoff = now - Window;

        var queue = _attempts.GetOrAdd(key, _ => new Queue<DateTime>());
        lock (queue)
        {
            while (queue.Count > 0 && queue.Peek() < cutoff)
                queue.Dequeue();

            if (queue.Count >= MaxAttempts)
            {
                var oldest = queue.Peek();
                retryAfterSeconds = (int)Math.Ceiling((oldest + Window - now).TotalSeconds);
                if (retryAfterSeconds < 1) retryAfterSeconds = 1;
                return false;
            }

            queue.Enqueue(now);
            return true;
        }
    }

    private static string HashEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
