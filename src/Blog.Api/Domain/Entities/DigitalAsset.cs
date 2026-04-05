namespace Blog.Api.Domain.Entities;

public class DigitalAsset
{
    public Guid DigitalAssetId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public User Creator { get; set; } = null!;
}
