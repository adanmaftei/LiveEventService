using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Users.User.Erase;

public class EraseUserCommand : IRequest<BaseResponse<bool>>
{
    public string UserId { get; set; } = string.Empty;
    public bool HardDelete { get; set; } = false; // default to anonymize/deactivate
}


