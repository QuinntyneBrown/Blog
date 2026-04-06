using Blog.Api.Common.Exceptions;
using Blog.Api.Features.Events.Queries;
using Blog.Api.Services;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Blog.Api.Features.Events.Commands;

public record UpdateEventCommand(
    Guid EventId,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime? EndDate,
    string TimeZoneId,
    string Location,
    string? ExternalUrl,
    int Version) : IRequest<EventDto>;

public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64)
            .Must(CreateEventCommandValidator.BeAValidTimeZone).WithMessage("'{PropertyValue}' is not a valid IANA time zone identifier.");
        RuleFor(x => x.Location).NotEmpty().MaximumLength(512);
        RuleFor(x => x.EndDate).Must((cmd, endDate) => endDate == null || endDate >= cmd.StartDate)
            .WithMessage("EndDate must be greater than or equal to StartDate.");
        RuleFor(x => x.ExternalUrl)
            .Must(url => url == null || url.Length > 0).WithMessage("ExternalUrl must not be an empty string.")
            .MaximumLength(2048)
            .Must(CreateEventCommandValidator.BeAValidHttpsUrl).When(x => !string.IsNullOrEmpty(x.ExternalUrl))
            .WithMessage("ExternalUrl must be a well-formed absolute HTTPS URL.");
        RuleFor(x => x.Version).GreaterThan(0);
    }
}

public class UpdateEventCommandHandler(
    IUnitOfWork uow,
    ISlugGenerator slugGenerator,
    ICacheInvalidator cacheInvalidator,
    ILogger<UpdateEventCommandHandler> logger) : IRequestHandler<UpdateEventCommand, EventDto>
{
    public async Task<EventDto> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await uow.Events.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{request.EventId}' was not found.");

        if (request.Version != ev.Version)
            throw new ConflictException("The event has been modified by another request. Please re-fetch and try again.");

        var oldSlug = ev.Slug;

        // Regenerate slug only if event has never been published (slug is frozen after first publish)
        if (ev.FirstPublishedAt == null)
        {
            var newSlug = slugGenerator.Generate(request.Title);
            if (string.IsNullOrWhiteSpace(newSlug))
                newSlug = ev.EventId.ToString("N");

            if (newSlug != ev.Slug)
            {
                if (await uow.Events.SlugExistsAsync(newSlug, ev.EventId, cancellationToken))
                    throw new ConflictException($"An event with slug '{newSlug}' already exists.");
                ev.Slug = newSlug;
            }
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
        ev.Title = request.Title;
        ev.Description = request.Description;
        ev.StartDate = request.StartDate;
        ev.EndDate = request.EndDate;
        ev.TimeZoneId = request.TimeZoneId;
        ev.StartDateUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(request.StartDate, DateTimeKind.Unspecified), timeZone);
        ev.EndDateUtc = request.EndDate.HasValue
            ? TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Unspecified), timeZone)
            : null;
        ev.Location = request.Location;
        ev.ExternalUrl = request.ExternalUrl;
        ev.UpdatedAt = DateTime.UtcNow;

        uow.Events.Update(ev);
        await uow.SaveChangesAsync(cancellationToken);

        if (ev.Published)
        {
            cacheInvalidator.InvalidateEvent(ev.Slug);
            if (oldSlug != ev.Slug)
                cacheInvalidator.InvalidateEvent(oldSlug);
        }

        return EventDto.FromEntity(ev);
    }
}
