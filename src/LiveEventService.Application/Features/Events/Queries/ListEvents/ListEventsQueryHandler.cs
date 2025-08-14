using LiveEventService.Core.Events;
using LiveEventService.Core.Users.User;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using AutoMapper;
using LiveEventService.Application.Features.Events.Queries.ListEvents;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using LiveEventService.Application.Features.Users.Queries.GetUsersByIdentityIds;

namespace LiveEventService.Application.Features.Events.Event.List;

public class ListEventsQueryHandler : IQueryHandler<ListEventsQuery, BaseResponse<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;

    public ListEventsQueryHandler(
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

    public async Task<BaseResponse<EventListDto>> Handle(ListEventsQuery request, CancellationToken cancellationToken)
    {
        // Try cache first
        string cacheKey = $"events:list:v1:p{request.PageNumber}:s{request.PageSize}:pub{request.IsPublished}:up{request.IsUpcoming}:org{request.OrganizerId ?? "anon"}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            var cachedDto = JsonSerializer.Deserialize<BaseResponse<EventListDto>>(cached);
            if (cachedDto != null)
            {
                // Metrics recorded in Infrastructure via a recorder if needed
                return cachedDto;
            }
        }

        // Build specification
        var spec = new ListEventsSpecification(request.IsPublished, request.OrganizerId, request.IsUpcoming);
        spec.ApplyPaging((request.PageNumber - 1) * request.PageSize, request.PageSize);

        // Get filtered and paged events using read-only query (no change tracking)
        var events = await _eventRepository.ListReadOnlyAsync(spec, cancellationToken);
        // Get total count for pagination (without paging)
        var countSpec = new ListEventsSpecification(request.IsPublished, request.OrganizerId, request.IsUpcoming);
        var totalCount = await _eventRepository.CountAsync(countSpec, cancellationToken);

        // Get organizer details for each event using specification-based query (no change tracking)
        var organizerIds = events.Select(e => e.OrganizerId).Distinct().ToList();
        var organizerLookup = new Dictionary<string, User>();

        if (organizerIds.Any())
        {
            var organizerSpec = new GetUsersByIdentityIdsSpecification(organizerIds);
            var organizers = await _userRepository.ListReadOnlyAsync(organizerSpec, cancellationToken);
            organizerLookup = organizers.ToDictionary(o => o.IdentityId);
        }

        // Map to DTOs
        var eventDtos = events.Select(e =>
        {
            var dto = _mapper.Map<EventDto>(e);
            if (organizerLookup.TryGetValue(e.OrganizerId, out var organizer))
            {
                dto.OrganizerName = $"{organizer.FirstName} {organizer.LastName}".Trim();
            }
            return dto;
        }).ToList();

        var result = new EventListDto
        {
            Items = eventDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var response = BaseResponse<EventListDto>.Succeeded(result);
        var serialized = JsonSerializer.Serialize(response);
        await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        }, cancellationToken);
        return response;
    }
}
