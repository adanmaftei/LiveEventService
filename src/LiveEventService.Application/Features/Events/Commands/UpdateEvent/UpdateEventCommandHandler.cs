using AutoMapper;
using LiveEventService.Core.Events;
using LiveEventService.Core.Users.User;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;

namespace LiveEventService.Application.Features.Events.Event.Update;

/// <summary>
/// Handles updates to existing events, including optional publish state changes.
/// </summary>
public class UpdateEventCommandHandler : ICommandHandler<UpdateEventCommand, BaseResponse<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateEventCommandHandler"/> class.
    /// </summary>
    /// <param name="eventRepository">The event repository for data access.</param>
    /// <param name="userRepository">The user repository for organizer data.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    public UpdateEventCommandHandler(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<BaseResponse<EventDto>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var existingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (existingEvent == null)
        {
            return BaseResponse<EventDto>.Failed("Event not found");
        }

        // Verify the user is the organizer
        if (existingEvent.OrganizerId != request.UserId)
        {
            return BaseResponse<EventDto>.Failed("You are not authorized to update this event");
        }

        // Update event properties
        existingEvent.UpdateDetails(
            request.Event.Title,
            request.Event.Description,
            request.Event.StartDateTime,
            request.Event.EndDateTime,
            request.Event.Capacity,
            request.Event.TimeZone,
            request.Event.Location);

        // Handle publish/unpublish if specified
        if (request.Event.IsPublished.HasValue)
        {
            if (request.Event.IsPublished.Value)
            {
                existingEvent.Publish();
            }
            else
            {
                existingEvent.Unpublish();
            }
        }

        await _eventRepository.UpdateAsync(existingEvent, cancellationToken);

        // Get organizer details
        var organizer = await _userRepository.GetByIdentityIdAsync(existingEvent.OrganizerId, cancellationToken);

        // Map to DTO
        var eventDto = _mapper.Map<EventDto>(existingEvent);
        eventDto.OrganizerName = organizer != null ? $"{organizer.FirstName} {organizer.LastName}".Trim() : string.Empty;

        return BaseResponse<EventDto>.Succeeded(eventDto, "Event updated successfully");
    }
}
