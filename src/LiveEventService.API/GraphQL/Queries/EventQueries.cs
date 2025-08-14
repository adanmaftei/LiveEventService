using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Application.Features.Events.Event.Get;
using LiveEventService.Application.Features.Events.Event.List;
using LiveEventService.Application.Features.Events.EventRegistration;
using LiveEventService.Application.Features.Events.EventRegistration.Get;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace LiveEventService.API.Events;

[ExtendObjectType(OperationTypeNames.Query)]
public class EventQueries
{
    public async Task<EventDto> GetEvent(
        [Service] IMediator mediator,
        [Service] IDistributedCache cache,
        Guid id,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"event:{id}:graphql";
        var (hit, cached) = await API.Utilities.CacheHelper.TryGetAsync<BaseResponse<EventDto>>(cache, cacheKey, cancellationToken);
        if (hit && cached?.Data != null)
        {
            return cached.Data;
        }

        var query = new GetEventQuery { EventId = id };
        var result = await mediator.Send(query, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Event not found");
        }

        await API.Utilities.CacheHelper.SetAsync(cache, cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
        return result.Data;
    }

    public async Task<EventListDto> GetEvents(
        [Service] IMediator mediator,
        int pageNumber = 1,
        int pageSize = 10,
        bool? isPublished = null,
        bool? isUpcoming = null,
        string? organizerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListEventsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            IsPublished = isPublished,
            IsUpcoming = isUpcoming,
            OrganizerId = organizerId
        };

        var result = await mediator.Send(query, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error retrieving events");
        }

        return result.Data;
    }

    public async Task<EventRegistrationListDto> GetEventRegistrations(
        [Service] IMediator mediator,
        Guid eventId,
        int pageNumber = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEventRegistrationsQuery
        {
            EventId = eventId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status
        };

        var result = await mediator.Send(query, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error retrieving event registrations");
        }

        return result.Data;
    }
}
