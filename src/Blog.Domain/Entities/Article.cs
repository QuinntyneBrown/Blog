namespace Blog.Domain.Entities;

public class Article
{
    public Guid ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public Guid? FeaturedImageId { get; set; }
    public DigitalAsset? FeaturedImage { get; set; }
    public bool Published { get; set; }
    public DateTime? DatePublished { get; set; }
    public int ReadingTimeMinutes { get; set; } = 1;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
