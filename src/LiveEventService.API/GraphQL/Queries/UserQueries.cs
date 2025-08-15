using System.Security.Claims;
using HotChocolate.Authorization;
using LiveEventService.Application.Features.Users.Queries.ExportUserData;
using LiveEventService.Application.Features.Users.User;
using LiveEventService.Application.Features.Users.User.Get;
using LiveEventService.Application.Features.Users.User.List;
using LiveEventService.Core.Common;
using MediatR;

namespace LiveEventService.API.Users;

/// <summary>
/// GraphQL queries for user-related operations.
/// Provides read access to user data with role-based authorization support.
/// </summary>
[ExtendObjectType(OperationTypeNames.Query)]
public class UserQueries
{
    /// <summary>
    /// Retrieves a single user by their unique identifier.
    /// </summary>
    /// <param name="mediator">The mediator for handling the query.</param>
    /// <param name="id">The unique identifier of the user to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user data if found; throws GraphQLException if not found.</returns>
    /// <exception cref="GraphQLException">Thrown when the user is not found or an error occurs.</exception>
    public async Task<UserDto> GetUser(
        [Service] IMediator mediator,
        string id,
        CancellationToken cancellationToken)
    {
        var query = new GetUserQuery { UserId = id };
        var result = await mediator.Send(query, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "User not found");
        }

        return result.Data;
    }

    /// <summary>
    /// Retrieves a paginated list of users with optional filtering.
    /// Supports searching by name/email and filtering by active status.
    /// </summary>
    /// <param name="mediator">The mediator for handling the query.</param>
    /// <param name="pageNumber">The page number for pagination (default: 1).</param>
    /// <param name="pageSize">The number of users per page (default: 20).</param>
    /// <param name="searchTerm">Optional search term for filtering users by name or email.</param>
    /// <param name="isActive">Optional filter for active/inactive users.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Paginated list of users matching the criteria.</returns>
    /// <exception cref="GraphQLException">Thrown when an error occurs retrieving users.</exception>
    public async Task<UserListDto> GetUsers(
        [Service] IMediator mediator,
        int pageNumber = 1,
        int pageSize = 20,
        string? searchTerm = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListUsersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            IsActive = isActive
        };

        var result = await mediator.Send(query, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error retrieving users");
        }

        return result.Data;
    }

    /// <summary>
    /// Retrieves the current authenticated user's profile.
    /// Uses the current user ID from the GraphQL context.
    /// </summary>
    /// <param name="mediator">The mediator for handling the query.</param>
    /// <param name="currentUserId">The current user's ID from the GraphQL context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The current user's profile data.</returns>
    /// <exception cref="GraphQLException">Thrown when the user is not found or an error occurs.</exception>
    public async Task<UserDto> GetCurrentUser(
        [Service] IMediator mediator,
        [GlobalState] string currentUserId,
        CancellationToken cancellationToken)
    {
        var query = new GetUserQuery { UserId = currentUserId };
        var result = await mediator.Send(query, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "User not found");
        }

        return result.Data;
    }

    /// <summary>
    /// Exports user data in JSON format for GDPR compliance.
    /// Requires authorization - users can only export their own data unless they are an admin.
    /// </summary>
    /// <param name="mediator">The mediator for handling the query.</param>
    /// <param name="currentUserId">The current user's ID from the GraphQL context.</param>
    /// <param name="claimsPrincipal">The user's claims for authorization checks.</param>
    /// <param name="id">The ID of the user whose data to export.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>JSON string containing the user's data.</returns>
    /// <exception cref="GraphQLException">Thrown when unauthorized or an error occurs.</exception>
    [Authorize]
    public async Task<string> ExportUserData(
        [Service] IMediator mediator,
        [GlobalState] string currentUserId,
        ClaimsPrincipal claimsPrincipal,
        string id,
        CancellationToken cancellationToken)
    {
        var isAdmin = claimsPrincipal.IsInRole(RoleNames.Admin);
        if (!isAdmin && id != currentUserId)
        {
            throw new GraphQLException("You are not authorized to export this user's data");
        }

        var result = await mediator.Send(new ExportUserDataQuery { UserId = id }, cancellationToken);
        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error exporting user data");
        }
        return result.Data.Json;
    }
}
