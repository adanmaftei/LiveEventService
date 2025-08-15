using MediatR;
using LiveEventService.Application.Common.Models;

namespace LiveEventService.Application.Features.Users.User.Create;

/// <summary>
/// Command to create a new user account from the provided details.
/// </summary>
public class CreateUserCommand : IRequest<BaseResponse<UserDto>>
{
    /// <summary>
    /// Gets or sets details of the user to create.
    /// </summary>
    public CreateUserDto User { get; set; } = null!;
}
