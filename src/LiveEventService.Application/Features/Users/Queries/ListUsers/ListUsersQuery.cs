using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Users.User.List;

public class ListUsersQuery : IRequest<BaseResponse<UserListDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}
