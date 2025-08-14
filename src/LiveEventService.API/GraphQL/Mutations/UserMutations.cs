using LiveEventService.Application.Features.Users.User.Create;
using LiveEventService.Application.Features.Users.User.Update;
using LiveEventService.Application.Features.Users.User;
using LiveEventService.Application.Features.Users.User.Erase;
using MediatR;
using HotChocolate.Authorization;
using LiveEventService.Core.Common;
using System.Security.Claims;

namespace LiveEventService.API.Users;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class UserMutations
{
    [Authorize]
    public async Task<UserDto> CreateUser(
        [Service] IMediator mediator,
        CreateUserInput input,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand
        {
            User = input
        };
        
        var result = await mediator.Send(command, cancellationToken);
        
        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error creating user");
        }
        
        return result.Data;
    }
    
    [Authorize]
    public async Task<UserDto> UpdateUser(
        [Service] IMediator mediator,
        UpdateUserInput input,
        [GlobalState] string currentUserId,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        // Allow self-update or admin update
        var isAdmin = claimsPrincipal.IsInRole(RoleNames.Admin);
        if (!isAdmin && input.Id != currentUserId)
        {
            throw new GraphQLException("You are not authorized to update this user's profile");
        }
        var command = new UpdateUserCommand
        {
            UserId = input.Id,
            User = input
        };
        var result = await mediator.Send(command, cancellationToken);
        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error updating user");
        }
        return result.Data;
    }

    [Authorize(Roles = [RoleNames.Admin])]
    public async Task<bool> EraseUser(
        [Service] IMediator mediator,
        string id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new EraseUserCommand { UserId = id, HardDelete = false }, cancellationToken);
        if (!result.Success)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error erasing user");
        }
        return true;
    }
}

public class CreateUserInput : CreateUserDto {}
public class UpdateUserInput : UpdateUserDto {}
