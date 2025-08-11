using AutoMapper;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Core.Events;
using MediatR;

namespace LiveEventService.Application.Features.Events.Commands.UnpublishEvent;

public class UnpublishEventCommand : IRequest<BaseResponse<EventDto>>
{
    public Guid EventId { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
}

public class UnpublishEventCommandHandler : ICommandHandler<UnpublishEventCommand, BaseResponse<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public UnpublishEventCommandHandler(
        IEventRepository eventRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<BaseResponse<EventDto>> Handle(UnpublishEventCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity == null)
            return BaseResponse<EventDto>.Failed("Event not found");

        if (!eventEntity.IsPublished)
            return BaseResponse<EventDto>.Failed("Event is already unpublished");

        // Unpublish the event
        eventEntity.Unpublish();
        await _eventRepository.UpdateAsync(eventEntity, cancellationToken);

        // Map to DTO
        var eventDto = _mapper.Map<EventDto>(eventEntity);

        return BaseResponse<EventDto>.Succeeded(eventDto, "Event unpublished successfully");
    }
} 
