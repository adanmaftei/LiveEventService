using FluentValidation;

namespace LiveEventService.Application.Features.Events.Event.Update;

public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("Event ID is required");
            
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
            
        RuleFor(x => x.Event)
            .NotNull().WithMessage("Event data is required");
            
        When(x => x.Event != null, () =>
        {
            RuleFor(x => x.Event.Title)
                .NotEmpty().When(x => x.Event.Title != null).WithMessage("Title cannot be empty")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters");
                
            RuleFor(x => x.Event.Description)
                .NotEmpty().When(x => x.Event.Description != null).WithMessage("Description cannot be empty")
                .MaximumLength(4000).WithMessage("Description must not exceed 4000 characters");
                
            RuleFor(x => x.Event.StartDateTime)
                .NotEmpty().When(x => x.Event.StartDateTime != default).WithMessage("Start date/time is required")
                .GreaterThan(DateTime.UtcNow).When(x => x.Event.StartDateTime != default)
                .WithMessage("Start date must be in the future");
                
            RuleFor(x => x.Event.EndDateTime)
                .NotEmpty().When(x => x.Event.EndDateTime != default).WithMessage("End date/time is required")
                .GreaterThan(x => x.Event.StartDateTime).When(x => x.Event.StartDateTime != default && x.Event.EndDateTime != default)
                .WithMessage("End date must be after start date");
                
            RuleFor(x => x.Event.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0")
                .LessThanOrEqualTo(10000).WithMessage("Capacity must not exceed 10,000");
                
            RuleFor(x => x.Event.TimeZone)
                .NotEmpty().When(x => !string.IsNullOrEmpty(x.Event.TimeZone)).WithMessage("Time zone cannot be empty");
                
            RuleFor(x => x.Event.Location)
                .NotEmpty().When(x => x.Event.Location != null).WithMessage("Location cannot be empty")
                .MaximumLength(500).WithMessage("Location must not exceed 500 characters");
        });
    }
}
