using Blog.Api.Common.Exceptions;
using Blog.Api.Features.Events.Queries;
using Blog.Api.Services;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Blog.Api.Features.Events.Commands;

public record CreateEventCommand(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime? EndDate,
    string TimeZoneId,
    string Location,
    string? ExternalUrl) : IRequest<EventDto>;

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64)
            .Must(BeAValidTimeZone).WithMessage("'{PropertyValue}' is not a valid IANA time zone identifier.");
        RuleFor(x => x.Location).NotEmpty().MaximumLength(512);
        RuleFor(x => x.EndDate).Must((cmd, endDate) => endDate == null || endDate >= cmd.StartDate)
            .WithMessage("EndDate must be greater than or equal to StartDate.");
        RuleFor(x => x.ExternalUrl).MaximumLength(2048)
            .Must(BeAValidHttpsUrl).When(x => !string.IsNullOrEmpty(x.ExternalUrl))
            .WithMessage("ExternalUrl must be a well-formed absolute HTTPS URL.");
    }

    private static bool BeAValidTimeZone(string timeZoneId)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
    }

    private static bool BeAValidHttpsUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }
}

public class CreateEventCommandHandler(
    IUnitOfWork uow,
    ISlugGenerator slugGenerator,
    ILogger<CreateEventCommandHandler> logger) : IRequestHandler<CreateEventCommand, EventDto>
{
    public async Task<EventDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid();
        var slug = slugGenerator.Generate(request.Title);

        if (string.IsNullOrWhiteSpace(slug))
            slug = eventId.ToString("N");

        if (await uow.Events.SlugExistsAsync(slug, cancellationToken: cancellationToken))
            throw new ConflictException($"An event with slug '{slug}' already exists.");

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
        var startDateUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(request.StartDate, DateTimeKind.Unspecified), timeZone);
        DateTime? endDateUtc = request.EndDate.HasValue
            ? TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Unspecified), timeZone)
            : null;

        var ev = new Event
        {
            EventId = eventId,
            Title = request.Title,
            Slug = slug,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TimeZoneId = request.TimeZoneId,
            StartDateUtc = startDateUtc,
            EndDateUtc = endDateUtc,
            Location = request.Location,
            ExternalUrl = request.ExternalUrl,
            Published = false,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await uow.Events.AddAsync(ev, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Business event {EventType} occurred: {@Details}",
            "event.created", new { ev.EventId, ev.Title });

        return EventDto.FromEntity(ev);
    }
}
