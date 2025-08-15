using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Users.User.Erase;

/// <summary>
/// Command to erase (hard delete) or anonymize/deactivate a user account.
/// </summary>
public class EraseUserCommand : IRequest<BaseResponse<bool>>
{
    /// <summary>
    /// Gets or sets identity ID (or GUID string) of the user to erase.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether when true, permanently deletes the user; otherwise deactivates and anonymizes.
    /// </summary>
    public bool HardDelete { get; set; } = false; // default to anonymize/deactivate
}
