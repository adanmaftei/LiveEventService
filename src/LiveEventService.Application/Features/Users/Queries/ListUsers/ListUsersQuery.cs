using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Users.User.List;

/// <summary>
/// Query to list users with optional search and active filters.
/// </summary>
public class ListUsersQuery : IRequest<BaseResponse<UserListDto>>
{
    /// <summary>
    /// Gets or sets 1-based page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets optional case-insensitive search term against name and email.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets optional active-state filter.
    /// </summary>
    public bool? IsActive { get; set; }
}
