using MediatR;
using LiveEventService.Application.Common.Models;

namespace LiveEventService.Application.Features.Users.User.Get;

/// <summary>
/// Query to fetch a single user by identity ID.
/// </summary>
public class GetUserQuery : IRequest<BaseResponse<UserDto>>
{
    /// <summary>
    /// Gets or sets identity ID of the user to fetch.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}
