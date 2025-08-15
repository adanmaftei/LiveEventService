using AutoMapper;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Core.Events;

namespace LiveEventService.Application.Features.Events.Commands.PublishEvent;

/// <summary>
/// Handles publishing of events by toggling the domain entity state and returning the updated DTO.
/// </summary>
public class PublishEventCommandHandler : ICommandHandler<PublishEventCommand, BaseResponse<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishEventCommandHandler"/> class.
    /// </summary>
    /// <param name="eventRepository">The event repository for data access.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    public PublishEventCommandHandler(
        IEventRepository eventRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<BaseResponse<EventDto>> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity == null)
        {
            return BaseResponse<EventDto>.Failed("Event not found");
        }

        if (eventEntity.IsPublished)
        {
            return BaseResponse<EventDto>.Failed("Event is already published");
        }

        // Publish the event
        eventEntity.Publish();
        await _eventRepository.UpdateAsync(eventEntity, cancellationToken);

        // Map to DTO
        var eventDto = _mapper.Map<EventDto>(eventEntity);

        return BaseResponse<EventDto>.Succeeded(eventDto, "Event published successfully");
    }
}
