using AutoMapper;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Core.Common;
using LiveEventService.Core.Events;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;
using IUserRepository = LiveEventService.Core.Users.User.IUserRepository;

namespace LiveEventService.Application.Features.Events.EventRegistration.Register;

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

        // Check if event is full BEFORE creating registration
        var confirmedRegistrationCount = await _eventRepository.GetRegistrationCountForEventAsync(request.EventId, cancellationToken);
        var isEventFull = confirmedRegistrationCount >= eventEntity.Capacity;

        // Create the registration
        var registration = new EventRegistrationEntity(eventEntity, user, request.Notes);
        
        // Set appropriate status based on event capacity
        if (isEventFull)
        {
            // Set status to waitlisted first
            registration.AddToWaitlist();
        }
        else
        {
            registration.Confirm(); // Auto-confirm if event has capacity
        }

        // Save registration first
        var createdRegistration = await _registrationRepository.AddAsync(registration, cancellationToken);
        
        // Assign waitlist position AFTER saving, based on database insertion order
        if (isEventFull)
        {
            var waitlistPosition = await _eventRepository.CalculateWaitlistPositionAsync(request.EventId, createdRegistration.Id, cancellationToken);
            createdRegistration.UpdateWaitlistPosition(waitlistPosition);
            await _registrationRepository.UpdateAsync(createdRegistration, cancellationToken);
        }
        
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
