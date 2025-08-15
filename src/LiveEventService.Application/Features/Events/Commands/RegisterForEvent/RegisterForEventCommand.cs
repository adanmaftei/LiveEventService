using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Core.Common;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;
using IUserRepository = LiveEventService.Core.Users.User.IUserRepository;
using MediatR;

namespace LiveEventService.Application.Features.Events.EventRegistration.Register;

/// <summary>
/// Command to register a user for an event; may auto-confirm or add to waitlist based on capacity.
/// </summary>
public class RegisterForEventCommand : IRequest<BaseResponse<EventRegistrationDto>>
{
    /// <summary>
    /// Gets or sets identifier of the target event.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets identity ID of the registering user.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional registration notes supplied by the user.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Command to cancel an existing event registration.
/// </summary>
public class CancelEventRegistrationCommand : IRequest<BaseResponse<bool>>
{
    /// <summary>
    /// Gets or sets identifier of the registration to cancel.
    /// </summary>
    public Guid RegistrationId { get; set; }

    /// <summary>
    /// Gets or sets identity ID of the requesting user.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the request is performed by an administrator.
    /// </summary>
    public bool IsAdmin { get; set; } = false;
}

/// <summary>
/// Handles cancellation of registrations, enforcing authorization and emitting domain events.
/// </summary>
public class CancelEventRegistrationCommandHandler : ICommandHandler<CancelEventRegistrationCommand, BaseResponse<bool>>
{
    private readonly IRepository<EventRegistrationEntity> _registrationRepository;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelEventRegistrationCommandHandler"/> class.
    /// </summary>
    /// <param name="registrationRepository">The registration repository for data access.</param>
    /// <param name="userRepository">The user repository for authorization checks.</param>
    public CancelEventRegistrationCommandHandler(
        IRepository<EventRegistrationEntity> registrationRepository,
        IUserRepository userRepository)
    {
        _registrationRepository = registrationRepository;
        _userRepository = userRepository;
    }

    public async Task<BaseResponse<bool>> Handle(CancelEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        var registration = await _registrationRepository.GetByIdAsync(request.RegistrationId, cancellationToken);
        if (registration == null)
        {
            return BaseResponse<bool>.Failed("Registration not found");
        }

        // Only the user or an admin can cancel
        if (!request.IsAdmin)
        {
            var user = await _userRepository.GetByIdentityIdAsync(request.UserId, cancellationToken);
            if (user == null || registration.UserId != user.Id)
            {
                return BaseResponse<bool>.Failed("Not authorized to cancel this registration");
            }
        }

        // Cancel the registration - domain events will handle waitlist promotion
        registration.Cancel();
        await _registrationRepository.UpdateAsync(registration, cancellationToken);

        return BaseResponse<bool>.Succeeded(true, "Registration cancelled");
    }
}
