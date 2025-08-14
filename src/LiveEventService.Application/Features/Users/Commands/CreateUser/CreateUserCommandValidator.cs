using FluentValidation;

namespace LiveEventService.Application.Features.Users.User.Create;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.User)
            .NotNull().WithMessage("User data is required");

        When(x => x.User != null, () =>
        {
            RuleFor(x => x.User.IdentityId)
                .NotEmpty().WithMessage("Identity ID is required");

            RuleFor(x => x.User.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email address is required")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters");

            RuleFor(x => x.User.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

            RuleFor(x => x.User.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

            RuleFor(x => x.User.PhoneNumber)
                .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.User.PhoneNumber))
                .WithMessage("Phone number must not exceed 20 characters");
        });
    }
}
