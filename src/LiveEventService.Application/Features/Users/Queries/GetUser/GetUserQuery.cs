using MediatR;
using LiveEventService.Application.Common.Models;

namespace LiveEventService.Application.Features.Users.User.Get;

public class GetUserQuery : IRequest<BaseResponse<UserDto>>
{
    public string UserId { get; set; } = string.Empty;
}
