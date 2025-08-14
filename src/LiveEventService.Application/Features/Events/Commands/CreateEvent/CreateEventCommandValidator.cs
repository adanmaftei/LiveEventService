using FluentValidation;

namespace LiveEventService.Application.Features.Events.Event.Create;

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Event)
            .NotNull().WithMessage("Event data is required");

        RuleFor(x => x.OrganizerId)
            .NotEmpty().WithMessage("Organizer ID is required");

        When(x => x.Event != null, () =>
        {
            RuleFor(x => x.Event.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

            RuleFor(x => x.Event.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(4000).WithMessage("Description must not exceed 4000 characters");

            RuleFor(x => x.Event.StartDateTime)
                .NotEmpty().WithMessage("Start date/time is required")
                .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future");

            RuleFor(x => x.Event.EndDateTime)
                .NotEmpty().WithMessage("End date/time is required")
                .GreaterThan(x => x.Event.StartDateTime).WithMessage("End date must be after start date");

            RuleFor(x => x.Event.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0")
                .LessThanOrEqualTo(10000).WithMessage("Capacity must not exceed 10,000");

            RuleFor(x => x.Event.TimeZone)
                .NotEmpty().WithMessage("Time zone is required");

            RuleFor(x => x.Event.Location)
                .NotEmpty().WithMessage("Location is required")
                .MaximumLength(500).WithMessage("Location must not exceed 500 characters");
        });
    }
}
