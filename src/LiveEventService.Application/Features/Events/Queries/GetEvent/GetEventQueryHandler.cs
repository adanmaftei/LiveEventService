using AutoMapper;
using LiveEventService.Core.Events;
using LiveEventService.Core.Users.User;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LiveEventService.Application.Features.Events.Event.Get;

public class GetEventQueryHandler : IQueryHandler<GetEventQuery, BaseResponse<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;

    public GetEventQueryHandler(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IMapper mapper,
        IDistributedCache cache)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<BaseResponse<EventDto>> Handle(GetEventQuery request, CancellationToken cancellationToken)
    {
        // Cache-aside: try cache
        string cacheKey = $"events:get:v1:{request.EventId}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            var cachedDto = JsonSerializer.Deserialize<BaseResponse<EventDto>>(cached);
            if (cachedDto != null)
            {
                // Metrics moved to Infrastructure; no direct dependency from Application
                return cachedDto;
            }
        }

        // Use read-only query since we're just displaying data
        var eventEntity = await _eventRepository.GetByIdReadOnlyAsync(request.EventId, cancellationToken);
        if (eventEntity == null)
        {
            return BaseResponse<EventDto>.Failed("Event not found");
        }

        // Get organizer details (read-only)
        var organizer = await _userRepository.GetByIdentityIdAsync(eventEntity.OrganizerId, cancellationToken);
        
        // Map to DTO
        var eventDto = _mapper.Map<EventDto>(eventEntity);
        eventDto.OrganizerName = organizer != null ? $"{organizer.FirstName} {organizer.LastName}".Trim() : string.Empty;
        
        var response = BaseResponse<EventDto>.Succeeded(eventDto);
        var serialized = JsonSerializer.Serialize(response);
        await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, cancellationToken);
        return response;
    }
}
