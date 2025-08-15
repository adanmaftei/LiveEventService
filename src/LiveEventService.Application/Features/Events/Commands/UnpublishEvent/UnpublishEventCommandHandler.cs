using AutoMapper;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Core.Events;

namespace LiveEventService.Application.Features.Events.Commands.UnpublishEvent;

/// <summary>
/// Handles unpublishing of events and returns the updated event DTO.
/// </summary>
public class UnpublishEventCommandHandler : ICommandHandler<UnpublishEventCommand, BaseResponse<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnpublishEventCommandHandler"/> class.
    /// </summary>
    /// <param name="eventRepository">The event repository for data access.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    public UnpublishEventCommandHandler(
        IEventRepository eventRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<BaseResponse<EventDto>> Handle(UnpublishEventCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity == null)
        {
            return BaseResponse<EventDto>.Failed("Event not found");
        }

        if (!eventEntity.IsPublished)
        {
            return BaseResponse<EventDto>.Failed("Event is already unpublished");
        }

        // Unpublish the event
        eventEntity.Unpublish();
        await _eventRepository.UpdateAsync(eventEntity, cancellationToken);

        // Map to DTO
        var eventDto = _mapper.Map<EventDto>(eventEntity);

        return BaseResponse<EventDto>.Succeeded(eventDto, "Event unpublished successfully");
    }
}
