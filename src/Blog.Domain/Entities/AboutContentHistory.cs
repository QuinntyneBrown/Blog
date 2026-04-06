namespace Blog.Domain.Entities;

public class AboutContentHistory
{
    public Guid AboutContentHistoryId { get; set; }
    public Guid AboutContentId { get; set; }
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public Guid? ProfileImageId { get; set; }
    public int Version { get; set; }
    public DateTime ArchivedAt { get; set; }
}
