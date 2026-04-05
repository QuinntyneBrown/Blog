namespace Blog.Domain.Entities;

public class AboutContent
{
    public Guid AboutContentId { get; set; }
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public Guid? ProfileImageId { get; set; }
    public DigitalAsset? ProfileImage { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
