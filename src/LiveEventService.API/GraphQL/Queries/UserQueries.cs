using LiveEventService.Application.Features.Users.User;
using MediatR;
using LiveEventService.Application.Features.Users.User.Get;
using LiveEventService.Application.Features.Users.User.List;

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
}
