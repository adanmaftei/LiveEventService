using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Users.User.Update;

public class UpdateUserCommand : IRequest<BaseResponse<UserDto>>
{
    public string UserId { get; set; } = string.Empty;
    public UpdateUserDto User { get; set; } = null!;
}
