using LiveEventService.Application.Features.Users.User.Create;
using LiveEventService.Application.Features.Users.User.Get;
using LiveEventService.Application.Features.Users.User.List;
using LiveEventService.Application.Features.Users.User.Update;
using LiveEventService.Core.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LiveEventService.API.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/users", async (
            [FromServices] IMediator mediator,
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] bool? isActive) =>
        {
            var query = new ListUsersQuery
            {
                PageNumber = pageNumber == 0 ? 1 : pageNumber,
                PageSize = pageSize == 0 ? 10 : pageSize,
                IsActive = isActive
            };
            var result = await mediator.Send(query);
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting("general");

        endpoints.MapGet("/api/users/me", async (
            [FromServices] IMediator mediator,
            HttpContext httpContext) =>
        {
            var userId = httpContext.User.Identity?.Name ?? string.Empty;
            var query = new GetUserQuery { UserId = userId };
            var result = await mediator.Send(query);
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization()
        .RequireRateLimiting("general");

        endpoints.MapGet("/api/users/{id}", async (
            [FromServices] IMediator mediator,
            string id) =>
        {
            var query = new GetUserQuery { UserId = id };
            var result = await mediator.Send(query);
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting("general");

        endpoints.MapPost("/api/users", async (
            [FromServices] IMediator mediator,
            [FromBody] CreateUserCommand command) =>
        {
            var result = await mediator.Send(command);
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting("general");

        endpoints.MapPut("/api/users/{id}", async (
            [FromServices] IMediator mediator,
            string id,
            [FromBody] UpdateUserCommand command,
            HttpContext httpContext) =>
        {
            // Users can update themselves, or admins can update anyone
            var currentUserId = httpContext.User.Identity?.Name ?? string.Empty;
            var isAdmin = httpContext.User.IsInRole(RoleNames.Admin);
            
            if (id != currentUserId && !isAdmin)
            {
                return Results.Forbid();
            }
            
            if (id != command.UserId)
                return Results.BadRequest("ID in route does not match ID in the request body");
            
            var result = await mediator.Send(command);
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        }).RequireAuthorization()
        .RequireRateLimiting("general");
    }
} 
