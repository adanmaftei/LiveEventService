using FluentValidation;

namespace LiveEventService.Application.Features.Users.User.Update;

/// <summary>
/// FluentValidation rules for <see cref="UpdateUserCommand"/>.
/// </summary>
public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.User)
            .NotNull().WithMessage("User data is required");

        When(x => x.User != null, () =>
        {
            RuleFor(x => x.User.FirstName)
                .NotEmpty().When(x => x.User.FirstName != null).WithMessage("First name cannot be empty")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

            RuleFor(x => x.User.LastName)
                .NotEmpty().When(x => x.User.LastName != null).WithMessage("Last name cannot be empty")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

            RuleFor(x => x.User.PhoneNumber)
                .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.User.PhoneNumber))
                .WithMessage("Phone number must not exceed 20 characters");
        });
    }
}
