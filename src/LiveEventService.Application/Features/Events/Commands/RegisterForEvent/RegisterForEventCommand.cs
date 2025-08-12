using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Core.Common;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;
using IUserRepository = LiveEventService.Core.Users.User.IUserRepository;
using MediatR;

namespace LiveEventService.Application.Features.Events.EventRegistration.Register;

public class RegisterForEventCommand : IRequest<BaseResponse<EventRegistrationDto>>
{
    public Guid EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CancelEventRegistrationCommand : IRequest<BaseResponse<bool>>
{
    public Guid RegistrationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } = false;
}

public class CancelEventRegistrationCommandHandler : ICommandHandler<CancelEventRegistrationCommand, BaseResponse<bool>>
{
    private readonly IRepository<EventRegistrationEntity> _registrationRepository;
    private readonly IUserRepository _userRepository;

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
