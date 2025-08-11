using AutoMapper;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.EventRegistration;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Common;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Application.Features.Events.Commands.ConfirmRegistration;

public class ConfirmRegistrationCommandHandler : ICommandHandler<ConfirmRegistrationCommand, BaseResponse<EventRegistrationDto>>
{
    private readonly IRepository<EventRegistrationEntity> _registrationRepository;
    private readonly IMapper _mapper;

    public ConfirmRegistrationCommandHandler(
        IRepository<EventRegistrationEntity> registrationRepository,
        IMapper mapper)
    {
        _registrationRepository = registrationRepository;
        _mapper = mapper;
    }

    public async Task<BaseResponse<EventRegistrationDto>> Handle(ConfirmRegistrationCommand request, CancellationToken cancellationToken)
    {
        var registration = await _registrationRepository.GetByIdAsync(request.RegistrationId, cancellationToken);
        if (registration == null)
            return BaseResponse<EventRegistrationDto>.Failed("Registration not found");

        if (registration.Status == RegistrationStatus.Confirmed)
            return BaseResponse<EventRegistrationDto>.Failed("Registration is already confirmed");

        if (registration.Status != RegistrationStatus.Waitlisted && registration.Status != RegistrationStatus.Pending)
            return BaseResponse<EventRegistrationDto>.Failed("Only waitlisted or pending registrations can be confirmed");

        // Confirm the registration
        registration.Confirm();
        await _registrationRepository.UpdateAsync(registration, cancellationToken);

        // Map to DTO
        var registrationDto = _mapper.Map<EventRegistrationDto>(registration);

        return BaseResponse<EventRegistrationDto>.Succeeded(registrationDto, "Registration confirmed successfully");
    }
} 
