using AutoMapper;
using LiveEventService.Core.Events;
using LiveEventService.Core.Users.User;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using EventEntity = LiveEventService.Core.Events.Event;

namespace LiveEventService.Application.Features.Events.Event.Create;

/// <summary>
/// Handles creation of events by validating the organizer, persisting the event, and mapping to DTO.
/// </summary>
public class CreateEventCommandHandler : ICommandHandler<CreateEventCommand, BaseResponse<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventCommandHandler"/> class.
    /// </summary>
    /// <param name="eventRepository">The event repository for data access.</param>
    /// <param name="userRepository">The user repository for organizer validation.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<BaseResponse<EventDto>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // Verify organizer exists
        var organizer = await _userRepository.GetByIdentityIdAsync(request.OrganizerId, cancellationToken);
        if (organizer == null)
        {
            return BaseResponse<EventDto>.Failed("Organizer not found");
        }

        var newEvent = new EventEntity(
            request.Event.Title,
            request.Event.Description,
            request.Event.StartDateTime,
            request.Event.EndDateTime,
            request.Event.Capacity,
            request.Event.TimeZone,
            request.Event.Location,
            request.OrganizerId);

        var createdEvent = await _eventRepository.AddAsync(newEvent, cancellationToken);

        // Map to DTO
        var eventDto = _mapper.Map<EventDto>(createdEvent);
        eventDto.OrganizerName = $"{organizer.FirstName} {organizer.LastName}".Trim();

        return BaseResponse<EventDto>.Succeeded(eventDto, "Event created successfully");
    }
}
