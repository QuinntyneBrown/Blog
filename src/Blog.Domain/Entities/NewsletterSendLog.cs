namespace Blog.Domain.Entities;

public class NewsletterSendLog
{
    public Guid NewsletterSendLogId { get; set; }
    public Guid NewsletterId { get; set; }
    public Newsletter Newsletter { get; set; } = null!;
    public Guid? SubscriberId { get; set; }
    public NewsletterSubscriber? Subscriber { get; set; }
    public string RecipientIdempotencyKey { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
