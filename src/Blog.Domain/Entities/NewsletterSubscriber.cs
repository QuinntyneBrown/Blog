namespace Blog.Domain.Entities;

public class NewsletterSubscriber
{
    public Guid SubscriberId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? ConfirmationTokenHash { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public bool Confirmed { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ResubscribedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
