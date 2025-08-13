using LiveEventService.Application.Features.Events.Event.Create;
using LiveEventService.Application.Features.Events.Event.Update;
using LiveEventService.Application.Features.Events.Event.Delete;
using LiveEventService.Application.Features.Events.Event.List;
using LiveEventService.Application.Features.Events.Event.Get;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Application.Features.Events.EventRegistration.Register;
using LiveEventService.Application.Features.Events.EventRegistration.Get;
using LiveEventService.Application.Features.Events.Commands.ConfirmRegistration;
using LiveEventService.Application.Features.Events.Commands.PublishEvent;
using LiveEventService.Application.Features.Events.Commands.UnpublishEvent;
using LiveEventService.Core.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.API.Constants;
using LiveEventService.Infrastructure.Telemetry;
using Microsoft.Extensions.Caching.Distributed;
using LiveEventService.Application.Common.Models;

namespace LiveEventService.API.Events;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/events", async (
            [FromServices] IMediator mediator,
            [FromServices] IDistributedCache cache,
            HttpContext httpContext,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isPublished = true,
            [FromQuery] bool? isUpcoming = true,
            CancellationToken ct = default) =>
        {
            var organizerId = httpContext.User.Identity?.IsAuthenticated == true ? httpContext.User.Identity?.Name : null;
            var cacheKey = $"events:list:{pageNumber}:{pageSize}:{isPublished}:{isUpcoming}:{organizerId}";
            var (hit, cached) = await API.Utilities.CacheHelper.TryGetAsync<BaseResponse<EventListDto>>(cache, cacheKey, ct);
            if (hit && cached != null)
            {
                return Results.Ok(cached);
            }
            var query = new ListEventsQuery
            {
                PageNumber = pageNumber <= 0 ? 1 : pageNumber,
                PageSize = pageSize <= 0 ? 10 : pageSize,
                IsPublished = isPublished,
                IsUpcoming = isUpcoming,
                OrganizerId = organizerId
            };
            var result = await mediator.Send(query, ct);
            if (result.Success)
            {
                await API.Utilities.CacheHelper.SetAsync(cache, cacheKey, result, TimeSpan.FromMinutes(2), ct);
                return Results.Ok(result);
            }
            return Results.BadRequest(new { result.Errors, result.Message });
        }).AllowAnonymous() // No authentication required for public event list
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapGet("/api/events/{id:guid}", async (
            [FromServices] IMediator mediator,
            [FromServices] IDistributedCache cache,
            Guid id,
            CancellationToken ct) =>
        {
            // Try read-through cache first
            var cacheKey = $"event:{id}";
            var (hit, cached) = await API.Utilities.CacheHelper.TryGetAsync<BaseResponse<EventDto>>(cache, cacheKey, ct);
            if (hit && cached != null)
            {
                return Results.Ok(cached);
            }

            var query = new GetEventQuery { EventId = id };
            var result = await mediator.Send(query, ct);
            if (result.Success && result.Data != null)
            {
                await API.Utilities.CacheHelper.SetAsync(cache, cacheKey, result, TimeSpan.FromMinutes(5), ct);
                return Results.Ok(result);
            }
            return Results.NotFound(new { result.Errors });
        }).AllowAnonymous() // No authentication required for public event details
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapPost("/api/events", async (
            [FromServices] IMediator mediator,
            [FromServices] IAuditLogger audit,
            [FromServices] LiveEventService.API.Utilities.IIdempotencyStore idempo,
            [FromBody] CreateEventCommand command,
            HttpContext httpContext) =>
        {
            // Idempotency: key per user + route + hash of payload
            var userId = httpContext.User.Identity?.Name ?? "anonymous";
            var headerKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
            var baseKey = $"idempo:{userId}:{httpContext.Request.Path}";
            var key = !string.IsNullOrWhiteSpace(headerKey)
                ? $"{baseKey}:{headerKey}"
                : $"{baseKey}:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(command))))}";
            if (!await idempo.TryClaimAsync(key, TimeSpan.FromMinutes(10), httpContext.RequestAborted))
            {
                return Results.StatusCode(StatusCodes.Status409Conflict);
            }
            command.OrganizerId = httpContext.User.Identity?.Name ?? string.Empty;
            var result = await mediator.Send(command);
            if (result.Success && result.Data?.Id is not null)
            {
                AppMetrics.EventsCreated.Add(1);
                await audit.LogAsync(new LiveEventService.Core.Common.AuditLogEntry
                {
                    Action = "CreateEvent",
                    EntityType = "Event",
                    EntityId = result.Data.Id.ToString(),
                    UserId = command.OrganizerId,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["title"] = command.Event.Title,
                        ["capacity"] = command.Event.Capacity
                    }
                });
            }
            return result.Success ? Results.Created($"/api/events/{result.Data?.Id}", result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapPut("/api/events/{id:guid}", async (
            [FromServices] IMediator mediator,
            [FromServices] IAuditLogger audit,
            [FromServices] IDistributedCache cache,
            Guid id,
            [FromBody] UpdateEventCommand command,
            HttpContext httpContext) =>
        {
            if (id != command.EventId)
            {
                return Results.BadRequest("ID in route does not match ID in the request body");
            }
            command.UserId = httpContext.User.Identity?.Name ?? string.Empty;
            var result = await mediator.Send(command);
            if (result.Success)
            {
                AppMetrics.EventsUpdated.Add(1);
                // Invalidate cache for this event
                await cache.RemoveAsync($"event:{id}", httpContext.RequestAborted);
                await cache.RemoveAsync($"event:{id}:graphql", httpContext.RequestAborted);
                await audit.LogAsync(new LiveEventService.Core.Common.AuditLogEntry
                {
                    Action = "UpdateEvent",
                    EntityType = "Event",
                    EntityId = id.ToString(),
                    UserId = command.UserId,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["capacity"] = command.Event.Capacity
                    }
                });
            }
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapDelete("/api/events/{id:guid}", async (
            [FromServices] IMediator mediator,
            [FromServices] IAuditLogger audit,
            [FromServices] IDistributedCache cache,
            Guid id,
            HttpContext httpContext) =>
        {
            var command = new DeleteEventCommand 
            { 
                EventId = id,
                UserId = httpContext.User.Identity?.Name ?? string.Empty
            };
            var result = await mediator.Send(command);
            if (result.Success)
            {
                AppMetrics.EventsDeleted.Add(1);
                await cache.RemoveAsync($"event:{id}", httpContext.RequestAborted);
                await cache.RemoveAsync($"event:{id}:graphql", httpContext.RequestAborted);
                await audit.LogAsync(new LiveEventService.Core.Common.AuditLogEntry
                {
                    Action = "DeleteEvent",
                    EntityType = "Event",
                    EntityId = id.ToString(),
                    UserId = command.UserId
                });
            }
            return result.Success ? Results.NoContent() : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapPost("/api/events/{eventId:guid}/register", async (
            [FromServices] IMediator mediator,
            [FromServices] LiveEventService.API.Utilities.IIdempotencyStore idempo,
            Guid eventId,
            [FromBody] RegisterForEventCommand command,
            HttpContext httpContext) =>
        {
            var userId = httpContext.User.Identity?.Name ?? "anonymous";
            var headerKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
            var baseKey = $"idempo:{userId}:{httpContext.Request.Path}";
            var idKey = !string.IsNullOrWhiteSpace(headerKey) ? $"{baseKey}:{headerKey}" : $"{baseKey}:{eventId}";
            if (!await idempo.TryClaimAsync(idKey, TimeSpan.FromMinutes(10), httpContext.RequestAborted))
            {
                return Results.StatusCode(StatusCodes.Status409Conflict);
            }
            command.EventId = eventId;
            command.UserId = httpContext.User.Identity?.Name ?? string.Empty;
            var result = await mediator.Send(command);
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        })
        .RequireAuthorization()
        .RequireRateLimiting(PolicyNames.Registration);

        endpoints.MapGet("/api/events/{eventId:guid}/registrations", async (
            [FromServices] IMediator mediator,
            Guid eventId,
            [FromQuery] string? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var query = new GetEventRegistrationsQuery
            {
                EventId = eventId,
                Status = status,
                PageNumber = pageNumber <= 0 ? 1 : pageNumber,
                PageSize = pageSize <= 0 ? 10 : pageSize
            };
            var result = await mediator.Send(query);
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapGet("/api/events/{eventId:guid}/waitlist", async (
            [FromServices] IMediator mediator,
            Guid eventId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var query = new GetEventRegistrationsQuery
            {
                EventId = eventId,
                Status = RegistrationStatus.Waitlisted.ToString(),
                PageNumber = pageNumber <= 0 ? 1 : pageNumber,
                PageSize = pageSize <= 0 ? 10 : pageSize
            };
            var result = await mediator.Send(query);
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapPost("/api/events/{eventId:guid}/registrations/{registrationId:guid}/confirm", async (
            [FromServices] IMediator mediator,
            [FromServices] IAuditLogger audit,
            [FromServices] LiveEventService.API.Utilities.IIdempotencyStore idempo,
            Guid eventId,
            Guid registrationId,
            HttpContext httpContext) =>
        {
            var userId = httpContext.User.Identity?.Name ?? "anonymous";
            var headerKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
            var baseKey = $"idempo:{userId}:{httpContext.Request.Path}";
            var idKey = !string.IsNullOrWhiteSpace(headerKey) ? $"{baseKey}:{headerKey}" : $"{baseKey}:{registrationId}";
            if (!await idempo.TryClaimAsync(idKey, TimeSpan.FromMinutes(10), httpContext.RequestAborted))
            {
                return Results.StatusCode(StatusCodes.Status409Conflict);
            }
            var command = new ConfirmRegistrationCommand
            {
                RegistrationId = registrationId,
                AdminUserId = httpContext.User.Identity?.Name ?? string.Empty
            };
            var result = await mediator.Send(command);
            if (result.Success)
            {
                AppMetrics.RegistrationsCreated.Add(1);
                await audit.LogAsync(new LiveEventService.Core.Common.AuditLogEntry
                {
                    Action = "ConfirmRegistration",
                    EntityType = "EventRegistration",
                    EntityId = registrationId.ToString(),
                    UserId = command.AdminUserId,
                    Metadata = new Dictionary<string, object?> { ["eventId"] = eventId }
                });
            }
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapPost("/api/events/{eventId:guid}/registrations/{registrationId:guid}/cancel", async (
            [FromServices] IMediator mediator,
            [FromServices] IAuditLogger audit,
            [FromServices] LiveEventService.API.Utilities.IIdempotencyStore idempo,
            Guid eventId,
            Guid registrationId,
            HttpContext httpContext) =>
        {
            var userId = httpContext.User.Identity?.Name ?? "anonymous";
            var headerKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
            var baseKey = $"idempo:{userId}:{httpContext.Request.Path}";
            var idKey = !string.IsNullOrWhiteSpace(headerKey) ? $"{baseKey}:{headerKey}" : $"{baseKey}:{registrationId}";
            if (!await idempo.TryClaimAsync(idKey, TimeSpan.FromMinutes(10), httpContext.RequestAborted))
            {
                return Results.StatusCode(StatusCodes.Status409Conflict);
            }
            var command = new CancelEventRegistrationCommand
            {
                RegistrationId = registrationId,
                UserId = httpContext.User.Identity?.Name ?? string.Empty,
                IsAdmin = true
            };
            var result = await mediator.Send(command);
            if (result.Success)
            {
                AppMetrics.RegistrationsCancelled.Add(1);
                await audit.LogAsync(new LiveEventService.Core.Common.AuditLogEntry
                {
                    Action = "CancelRegistration",
                    EntityType = "EventRegistration",
                    EntityId = registrationId.ToString(),
                    UserId = command.UserId,
                    Metadata = new Dictionary<string, object?> { ["eventId"] = eventId }
                });
            }
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapPost("/api/events/{eventId:guid}/publish", async (
            [FromServices] IMediator mediator,
            [FromServices] IAuditLogger audit,
            [FromServices] IDistributedCache cache,
            Guid eventId,
            HttpContext httpContext) =>
        {
            var command = new PublishEventCommand
            {
                EventId = eventId,
                AdminUserId = httpContext.User.Identity?.Name ?? string.Empty
            };
            var result = await mediator.Send(command);
            if (result.Success)
            {
                AppMetrics.EventsPublished.Add(1);
                await cache.RemoveAsync($"event:{eventId}", httpContext.RequestAborted);
                await cache.RemoveAsync($"event:{eventId}:graphql", httpContext.RequestAborted);
                await audit.LogAsync(new LiveEventService.Core.Common.AuditLogEntry
                {
                    Action = "PublishEvent",
                    EntityType = "Event",
                    EntityId = eventId.ToString(),
                    UserId = command.AdminUserId
                });
            }
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin);

        endpoints.MapPost("/api/events/{eventId:guid}/unpublish", async (
            [FromServices] IMediator mediator,
            [FromServices] IAuditLogger audit,
            [FromServices] IDistributedCache cache,
            Guid eventId,
            HttpContext httpContext) =>
        {
            var command = new UnpublishEventCommand
            {
                EventId = eventId,
                AdminUserId = httpContext.User.Identity?.Name ?? string.Empty
            };
            var result = await mediator.Send(command);
            if (result.Success)
            {
                AppMetrics.EventsUnpublished.Add(1);
                await cache.RemoveAsync($"event:{eventId}", httpContext.RequestAborted);
                await cache.RemoveAsync($"event:{eventId}:graphql", httpContext.RequestAborted);
                await audit.LogAsync(new LiveEventService.Core.Common.AuditLogEntry
                {
                    Action = "UnpublishEvent",
                    EntityType = "Event",
                    EntityId = eventId.ToString(),
                    UserId = command.AdminUserId
                });
            }
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin);
    }
} 
