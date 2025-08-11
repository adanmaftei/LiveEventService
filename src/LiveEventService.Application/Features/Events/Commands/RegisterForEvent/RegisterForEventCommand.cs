using AutoMapper;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Application.Features.Events.Queries.GetEventRegistrations;
using LiveEventService.Core.Common;
using LiveEventService.Core.Events;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;
using LiveEventService.Core.Registrations.EventRegistration;
using IUserRepository = LiveEventService.Core.Users.User.IUserRepository;
using MediatR;

namespace LiveEventService.Application.Features.Events.EventRegistration.Register;

public class RegisterForEventCommand : IRequest<BaseResponse<EventRegistrationDto>>
{
    public Guid EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class RegisterForEventCommandHandler : ICommandHandler<RegisterForEventCommand, BaseResponse<EventRegistrationDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<EventRegistrationEntity> _registrationRepository;
    private readonly IMapper _mapper;

    public RegisterForEventCommandHandler(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IRepository<EventRegistrationEntity> registrationRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _registrationRepository = registrationRepository;
        _mapper = mapper;
    }

    public async Task<BaseResponse<EventRegistrationDto>> Handle(
        RegisterForEventCommand request, 
        CancellationToken cancellationToken)
    {
        // Get the event
        var eventEntity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity == null)
        {
            return BaseResponse<EventRegistrationDto>.Failed("Event not found");
        }

        // Check if event is published
        if (!eventEntity.IsPublished)
        {
            return BaseResponse<EventRegistrationDto>.Failed("This event is not currently accepting registrations");
        }

        // Validate event timing (only allow registration for future events)
        if (eventEntity.StartDate <= DateTime.UtcNow)
        {
            return BaseResponse<EventRegistrationDto>.Failed("This event has already started");
        }

        // Get the user
        var user = await _userRepository.GetByIdentityIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return BaseResponse<EventRegistrationDto>.Failed("User not found");
        }

        // Check if user is already registered
        var isRegistered = await _eventRepository.IsUserRegisteredForEventAsync(request.EventId, user.Id, cancellationToken);
        if (isRegistered)
        {
            return BaseResponse<EventRegistrationDto>.Failed("You are already registered for this event");
        }

        // Create the registration
        var registration = new EventRegistrationEntity(eventEntity, user, request.Notes);
        
        // Add to waitlist if event is full
        if (eventEntity.IsFull())
        {
            var waitlistPosition = await _eventRepository.GetRegistrationCountForEventAsync(request.EventId, cancellationToken) + 1;
            registration.UpdateWaitlistPosition(waitlistPosition);
        }

        var createdRegistration = await _registrationRepository.AddAsync(registration, cancellationToken);
        
        // Map to DTO
        var registrationDto = _mapper.Map<EventRegistrationDto>(createdRegistration);
        registrationDto.UserName = $"{user.FirstName} {user.LastName}".Trim();
        registrationDto.UserEmail = user.Email;
        
        var message = registration.IsWaitlisted() 
            ? "You have been added to the waitlist" 
            : "You have been successfully registered for the event";
        
        return BaseResponse<EventRegistrationDto>.Succeeded(registrationDto, message);
    }
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
            return BaseResponse<bool>.Failed("Registration not found");

        // Only the user or an admin can cancel
        if (!request.IsAdmin)
        {
            var user = await _userRepository.GetByIdentityIdAsync(request.UserId, cancellationToken);
            if (user == null || registration.UserId != user.Id)
                return BaseResponse<bool>.Failed("Not authorized to cancel this registration");
        }

        var wasConfirmed = registration.Status == RegistrationStatus.Confirmed;
        registration.Cancel();
        await _registrationRepository.UpdateAsync(registration, cancellationToken);

        // If this was a confirmed registration, promote the next waitlisted registration
        if (wasConfirmed)
        {
            // Get all waitlisted registrations for this event, ordered by PositionInQueue
            var waitlisted = (await _registrationRepository.ListAsync(
                new GetEventRegistrationsSpecification(registration.EventId, RegistrationStatus.Waitlisted.ToString(), null),
                cancellationToken)).OrderBy(r => r.PositionInQueue).ToList();
            if (waitlisted.Any())
            {
                var promote = waitlisted.First();
                promote.Confirm();
                await _registrationRepository.UpdateAsync(promote, cancellationToken);
                // Update positions for remaining waitlisted
                int pos = 1;
                foreach (var w in waitlisted.Skip(1))
                {
                    w.UpdateWaitlistPosition(pos++);
                    await _registrationRepository.UpdateAsync(w, cancellationToken);
                }
            }
        }
        return BaseResponse<bool>.Succeeded(true, "Registration cancelled");
    }
}
