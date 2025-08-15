using LiveEventService.Core.Events;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;

namespace LiveEventService.Application.Features.Events.Event.Delete;

/// <summary>
/// Handles deletion of events, ensuring authorization and no dependent registrations exist.
/// </summary>
public class DeleteEventCommandHandler : ICommandHandler<DeleteEventCommand, BaseResponse<bool>>
{
    private readonly IEventRepository _eventRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteEventCommandHandler"/> class.
    /// </summary>
    /// <param name="eventRepository">The event repository for data access.</param>
    public DeleteEventCommandHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    /// <inheritdoc />
    public async Task<BaseResponse<bool>> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var existingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (existingEvent == null)
        {
            return BaseResponse<bool>.Failed("Event not found");
        }

        // Verify the user is the organizer
        if (existingEvent.OrganizerId != request.UserId)
        {
            return BaseResponse<bool>.Failed("You are not authorized to delete this event");
        }

        // Check if there are any registrations
        var registrationCount = await _eventRepository.GetRegistrationCountForEventAsync(request.EventId, cancellationToken);
        if (registrationCount > 0)
        {
            return BaseResponse<bool>.Failed("Cannot delete an event with existing registrations");
        }

        await _eventRepository.DeleteAsync(existingEvent, cancellationToken);
        return BaseResponse<bool>.Succeeded(true, "Event deleted successfully");
    }
}
