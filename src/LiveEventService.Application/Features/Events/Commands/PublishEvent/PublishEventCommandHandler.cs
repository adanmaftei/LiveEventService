using AutoMapper;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Core.Events;

namespace LiveEventService.Application.Features.Events.Commands.PublishEvent;

public class PublishEventCommandHandler : ICommandHandler<PublishEventCommand, BaseResponse<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public PublishEventCommandHandler(
        IEventRepository eventRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

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
