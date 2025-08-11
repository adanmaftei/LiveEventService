using MediatR;
using LiveEventService.Application.Common.Models;

namespace LiveEventService.Application.Features.Users.User.Create;

public class CreateUserCommand : IRequest<BaseResponse<UserDto>>
{
    public CreateUserDto User { get; set; } = null!;
}
