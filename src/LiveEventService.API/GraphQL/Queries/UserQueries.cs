using LiveEventService.Application.Features.Users.User;
using MediatR;
using LiveEventService.Application.Features.Users.User.Get;
using LiveEventService.Application.Features.Users.User.List;
using LiveEventService.Application.Features.Users.Queries.ExportUserData;
using HotChocolate.Authorization;
using System.Security.Claims;
using LiveEventService.Core.Common;

namespace LiveEventService.API.Users;

[ExtendObjectType(OperationTypeNames.Query)]
public class UserQueries
{
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
