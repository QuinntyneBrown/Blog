namespace Blog.Domain.Entities;

public class AboutContent
{
    public static readonly Guid WellKnownId = new("00000000-0000-0000-0000-000000000001");

    public Guid AboutContentId { get; set; }
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public Guid? ProfileImageId { get; set; }
    public DigitalAsset? ProfileImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; } = 1;
}
