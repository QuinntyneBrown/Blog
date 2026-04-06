namespace Blog.Domain.Entities;

public class Event
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? ExternalUrl { get; set; }
    public bool Published { get; set; }
    public DateTime? FirstPublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; } = 1;
}
