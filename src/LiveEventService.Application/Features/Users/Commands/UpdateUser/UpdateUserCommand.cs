using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Users.User.Update;

/// <summary>
/// Command to update a user's profile details.
/// </summary>
public class UpdateUserCommand : IRequest<BaseResponse<UserDto>>
{
    /// <summary>
    /// Gets or sets identity ID of the user to update.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets new values for the user; null properties are ignored.
    /// </summary>
    public UpdateUserDto User { get; set; } = null!;
}
