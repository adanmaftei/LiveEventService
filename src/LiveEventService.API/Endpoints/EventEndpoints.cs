using LiveEventService.Application.Features.Events.Event.Create;
using LiveEventService.Application.Features.Events.Event.Update;
using LiveEventService.Application.Features.Events.Event.Delete;
using LiveEventService.Application.Features.Events.Event.List;
using LiveEventService.Application.Features.Events.Event.Get;
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

namespace LiveEventService.API.Events;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/events", async (
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isPublished = true,
            [FromQuery] bool? isUpcoming = true) =>
        {
            var query = new ListEventsQuery
            {
                PageNumber = pageNumber <= 0 ? 1 : pageNumber,
                PageSize = pageSize <= 0 ? 10 : pageSize,
                IsPublished = isPublished,
                IsUpcoming = isUpcoming,
                OrganizerId = httpContext.User.Identity?.IsAuthenticated == true ? httpContext.User.Identity?.Name : null
            };
            var result = await mediator.Send(query);
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors, result.Message });
        }).AllowAnonymous() // No authentication required for public event list
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapGet("/api/events/{id:guid}", async (
            [FromServices] IMediator mediator,
            Guid id) =>
        {
            var query = new GetEventQuery { EventId = id };
            var result = await mediator.Send(query);
            return result.Success ? Results.Ok(result) : Results.NotFound(new { result.Errors });
        }).AllowAnonymous() // No authentication required for public event details
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapPost("/api/events", async (
            [FromServices] IMediator mediator,
            [FromServices] LiveEventService.Application.Common.IAuditLogger audit,
            [FromBody] CreateEventCommand command,
            HttpContext httpContext) =>
        {
            command.OrganizerId = httpContext.User.Identity?.Name ?? string.Empty;
            var result = await mediator.Send(command);
            if (result.Success && result.Data?.Id is not null)
            {
                AppMetrics.EventsCreated.Add(1);
                await audit.LogAsync(new LiveEventService.Application.Common.AuditLogEntry
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
            [FromServices] LiveEventService.Application.Common.IAuditLogger audit,
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
                await audit.LogAsync(new LiveEventService.Application.Common.AuditLogEntry
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
            [FromServices] LiveEventService.Application.Common.IAuditLogger audit,
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
                await audit.LogAsync(new LiveEventService.Application.Common.AuditLogEntry
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
            Guid eventId,
            [FromBody] RegisterForEventCommand command,
            HttpContext httpContext) =>
        {
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
            [FromServices] LiveEventService.Application.Common.IAuditLogger audit,
            Guid eventId,
            Guid registrationId,
            HttpContext httpContext) =>
        {
            var command = new ConfirmRegistrationCommand
            {
                RegistrationId = registrationId,
                AdminUserId = httpContext.User.Identity?.Name ?? string.Empty
            };
            var result = await mediator.Send(command);
            if (result.Success)
            {
                AppMetrics.RegistrationsCreated.Add(1);
                await audit.LogAsync(new LiveEventService.Application.Common.AuditLogEntry
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
            [FromServices] LiveEventService.Application.Common.IAuditLogger audit,
            Guid eventId,
            Guid registrationId,
            HttpContext httpContext) =>
        {
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
                await audit.LogAsync(new LiveEventService.Application.Common.AuditLogEntry
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
            [FromServices] LiveEventService.Application.Common.IAuditLogger audit,
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
                await audit.LogAsync(new LiveEventService.Application.Common.AuditLogEntry
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
            [FromServices] LiveEventService.Application.Common.IAuditLogger audit,
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
                await audit.LogAsync(new LiveEventService.Application.Common.AuditLogEntry
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
