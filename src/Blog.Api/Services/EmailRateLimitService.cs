using System.Collections.Concurrent;

namespace Blog.Api.Services;

/// <summary>
/// In-memory sliding-window rate limiter scoped to a normalized email address.
/// Enforces a maximum of 5 login attempts per email within any 15-minute window.
/// Thread-safe via <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class EmailRateLimitService : IEmailRateLimitService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    // email (normalized) -> ordered queue of attempt timestamps
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _attempts = new(StringComparer.OrdinalIgnoreCase);

    public bool TryAcquire(string email)
    {
        var key = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var cutoff = now - Window;

        var queue = _attempts.GetOrAdd(key, _ => new Queue<DateTime>());

        lock (queue)
        {
            // Remove timestamps that have slid out of the window
            while (queue.Count > 0 && queue.Peek() < cutoff)
                queue.Dequeue();

            if (queue.Count >= MaxAttempts)
                return false;

            queue.Enqueue(now);
            return true;
        }
    }
}
