using System.Text;
using LiveEventService.API.Constants;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Users.Queries.ExportUserData;
using LiveEventService.Application.Features.Users.User;
using LiveEventService.Application.Features.Users.User.Create;
using LiveEventService.Application.Features.Users.User.Erase;
using LiveEventService.Application.Features.Users.User.Get;
using LiveEventService.Application.Features.Users.User.List;
using LiveEventService.Application.Features.Users.User.Update;
using LiveEventService.Core.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace LiveEventService.API.Users;

/// <summary>
/// Maps REST endpoints for user management: query, self profile, CRUD, export and erasure.
/// Enforces role-based access controls and rate limits; leverages cache for profile reads.
/// </summary>
public static class UserEndpoints
{
    /// <summary>
    /// Registers all user-related minimal API endpoints on the provided route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
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
        })
        .WithTags("Users")
        .WithSummary("List users")
        .WithDescription("Retrieves a paginated list of users, optionally filtered by active status. Admin only.")
        .Produces<BaseResponse<UserListDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapGet("/api/users/me", async (
            [FromServices] IMediator mediator,
            [FromServices] IDistributedCache cache,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.Identity?.Name ?? string.Empty;
            var cacheKey = $"user:{userId}";
            var (hit, cached) = await API.Utilities.CacheHelper.TryGetAsync<BaseResponse<UserDto>>(cache, cacheKey, ct);
            if (hit && cached != null)
            {
                return Results.Ok(cached);
            }
            var query = new GetUserQuery { UserId = userId };
            var result = await mediator.Send(query, ct);
            if (result.Success)
            {
                await API.Utilities.CacheHelper.SetAsync(cache, cacheKey, result, TimeSpan.FromMinutes(10), ct);
                return Results.Ok(result);
            }
            return Results.BadRequest(new { result.Errors });
        })
        .WithTags("Users")
        .WithSummary("Get current user profile")
        .WithDescription("Retrieves the authenticated user's profile, with caching.")
        .Produces<BaseResponse<UserDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization()
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapGet("/api/users/{id}", async (
            [FromServices] IMediator mediator,
            [FromServices] IDistributedCache cache,
            string id,
            CancellationToken ct) =>
        {
            var cacheKey = $"user:{id}";
            var (hit, cached) = await API.Utilities.CacheHelper.TryGetAsync<BaseResponse<UserDto>>(cache, cacheKey, ct);
            if (hit && cached != null)
            {
                return Results.Ok(cached);
            }
            var query = new GetUserQuery { UserId = id };
            var result = await mediator.Send(query, ct);
            if (result.Success)
            {
                await API.Utilities.CacheHelper.SetAsync(cache, cacheKey, result, TimeSpan.FromMinutes(10), ct);
                return Results.Ok(result);
            }
            return Results.BadRequest(new { result.Errors });
        })
        .WithTags("Users")
        .WithSummary("Get user by ID")
        .WithDescription("Retrieves a user profile by ID. Admin only.")
        .Produces<BaseResponse<UserDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapPost("/api/users", async (
            [FromServices] IMediator mediator,
            [FromBody] CreateUserCommand command) =>
        {
            var result = await mediator.Send(command);
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        })
        .WithTags("Users")
        .WithSummary("Create user")
        .WithDescription("Creates a new user. Admin only.")
        .Accepts<CreateUserCommand>("application/json")
        .Produces<BaseResponse<UserDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting(PolicyNames.General);

        endpoints.MapPut("/api/users/{id}", async (
            [FromServices] IMediator mediator,
            [FromServices] IDistributedCache cache,
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
            {
                return Results.BadRequest("ID in route does not match ID in the request body");
            }

            var result = await mediator.Send(command);
            if (result.Success)
            {
                await cache.RemoveAsync($"user:{id}", httpContext.RequestAborted);
            }
            return result.Success ? Results.Ok(result) : Results.BadRequest(new { result.Errors });
        })
        .WithTags("Users")
        .WithSummary("Update user")
        .WithDescription("Updates a user. Users can update themselves; admins can update any user.")
        .Accepts<UpdateUserCommand>("application/json")
        .Produces<BaseResponse<UserDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .RequireAuthorization()
        .RequireRateLimiting(PolicyNames.General);

        // User data export (DSAR) - user can export their own data; admin can export any
        endpoints.MapGet("/api/users/{id}/export", async (
            [FromServices] IMediator mediator,
            string id,
            HttpContext httpContext) =>
        {
            var currentUserId = httpContext.User.Identity?.Name ?? string.Empty;
            var isAdmin = httpContext.User.IsInRole(RoleNames.Admin);
            if (id != currentUserId && !isAdmin)
            {
                return Results.Forbid();
            }

            var result = await mediator.Send(new ExportUserDataQuery { UserId = id });
            if (!result.Success || result.Data == null)
            {
                return Results.BadRequest(new { result.Errors });
            }
            var bytes = Encoding.UTF8.GetBytes(result.Data.Json);
            return Results.File(bytes, "application/json", $"user-{id}-export.json");
        })
        .WithTags("Users")
        .WithSummary("Export user data")
        .WithDescription("Exports user data in JSON format for the specified user. Users can export themselves; admins can export any user.")
        .Produces(StatusCodes.Status200OK, contentType: "application/json")
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .RequireAuthorization()
        .RequireRateLimiting(PolicyNames.General);

        // User erasure (DSAR) - admin only; optional self-delete could be policy-dependent
        endpoints.MapDelete("/api/users/{id}", async (
            [FromServices] IMediator mediator,
            string id) =>
        {
            var result = await mediator.Send(new EraseUserCommand { UserId = id, HardDelete = false });
            return result.Success ? Results.NoContent() : Results.BadRequest(new { result.Errors });
        })
        .WithTags("Users")
        .WithSummary("Erase user")
        .WithDescription("Erases a user (policy-based soft delete). Admin only.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization(RoleNames.Admin)
        .RequireRateLimiting(PolicyNames.General);
    }
}
