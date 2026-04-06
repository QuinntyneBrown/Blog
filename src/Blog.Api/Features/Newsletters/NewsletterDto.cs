namespace Blog.Api.Features.Newsletters;

public record NewsletterDto(
    Guid NewsletterId, string Subject, string? Slug, string Body, string BodyHtml,
    string Status, DateTime? DateSent, DateTime CreatedAt, DateTime UpdatedAt, int Version);

public record NewsletterListDto(
    Guid NewsletterId, string Subject, string? Slug, string Status, DateTime? DateSent, DateTime CreatedAt);

public record NewsletterArchiveDto(string Subject, string Slug, DateTime? DateSent);

public record NewsletterArchiveDetailDto(string Subject, string Slug, string BodyHtml, DateTime? DateSent);
