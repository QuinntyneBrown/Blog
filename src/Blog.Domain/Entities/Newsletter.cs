namespace Blog.Domain.Entities;

public class Newsletter
{
    public Guid NewsletterId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Body { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public NewsletterStatus Status { get; set; } = NewsletterStatus.Draft;
    public DateTime? DateSent { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
