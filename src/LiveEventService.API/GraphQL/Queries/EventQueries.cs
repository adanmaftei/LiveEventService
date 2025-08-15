using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Application.Features.Events.Event.Get;
using LiveEventService.Application.Features.Events.Event.List;
using LiveEventService.Application.Features.Events.EventRegistration;
using LiveEventService.Application.Features.Events.EventRegistration.Get;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace LiveEventService.API.Events;

/// <summary>
/// GraphQL queries for event-related operations.
/// Provides read access to events, event lists, and event registrations with caching support.
/// </summary>
[ExtendObjectType(OperationTypeNames.Query)]
public class EventQueries
{
    /// <summary>
    /// Retrieves a single event by its unique identifier.
    /// Implements caching to improve performance for frequently accessed events.
    /// </summary>
    /// <param name="mediator">The mediator for handling the query.</param>
    /// <param name="cache">Distributed cache for storing event data.</param>
    /// <param name="id">The unique identifier of the event to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The event data if found; throws GraphQLException if not found.</returns>
    /// <exception cref="GraphQLException">Thrown when the event is not found or an error occurs.</exception>
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

    /// <summary>
    /// Retrieves a paginated list of events with optional filtering.
    /// Supports filtering by publication status, upcoming events, and organizer.
    /// </summary>
    /// <param name="mediator">The mediator for handling the query.</param>
    /// <param name="pageNumber">The page number for pagination (default: 1).</param>
    /// <param name="pageSize">The number of events per page (default: 10).</param>
    /// <param name="isPublished">Optional filter for published/unpublished events.</param>
    /// <param name="isUpcoming">Optional filter for upcoming events only.</param>
    /// <param name="organizerId">Optional filter for events by specific organizer.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Paginated list of events matching the criteria.</returns>
    /// <exception cref="GraphQLException">Thrown when an error occurs retrieving events.</exception>
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

    /// <summary>
    /// Retrieves a paginated list of registrations for a specific event.
    /// Supports filtering by registration status and pagination.
    /// </summary>
    /// <param name="mediator">The mediator for handling the query.</param>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="pageNumber">The page number for pagination (default: 1).</param>
    /// <param name="pageSize">The number of registrations per page (default: 20).</param>
    /// <param name="status">Optional filter for registration status (e.g., "Confirmed", "Waitlisted").</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Paginated list of event registrations.</returns>
    /// <exception cref="GraphQLException">Thrown when an error occurs retrieving registrations.</exception>
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
