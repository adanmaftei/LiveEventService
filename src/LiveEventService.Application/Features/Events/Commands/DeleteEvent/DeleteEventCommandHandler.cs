using LiveEventService.Core.Events;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;

namespace LiveEventService.Application.Features.Events.Event.Delete;

public class DeleteEventCommandHandler : ICommandHandler<DeleteEventCommand, BaseResponse<bool>>
{
    private readonly IEventRepository _eventRepository;

    public DeleteEventCommandHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

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
