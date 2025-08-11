using AutoMapper;
using LiveEventService.Core.Events;
using LiveEventService.Core.Users.User;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using MediatR;

namespace LiveEventService.Application.Features.Events.Event.Get;

public class GetEventQuery : IRequest<BaseResponse<EventDto>>
{
    public Guid EventId { get; set; }
}

public class GetEventQueryHandler : IQueryHandler<GetEventQuery, BaseResponse<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetEventQueryHandler(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<BaseResponse<EventDto>> Handle(GetEventQuery request, CancellationToken cancellationToken)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity == null)
        {
            return BaseResponse<EventDto>.Failed("Event not found");
        }

        // Get organizer details
        var organizer = await _userRepository.GetByIdentityIdAsync(eventEntity.OrganizerId, cancellationToken);
        
        // Map to DTO
        var eventDto = _mapper.Map<EventDto>(eventEntity);
        eventDto.OrganizerName = organizer != null ? $"{organizer.FirstName} {organizer.LastName}".Trim() : string.Empty;
        
        return BaseResponse<EventDto>.Succeeded(eventDto);
    }
}
