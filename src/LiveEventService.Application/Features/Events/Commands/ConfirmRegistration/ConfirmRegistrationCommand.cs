using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.EventRegistration;
using MediatR;

namespace LiveEventService.Application.Features.Events.Commands.ConfirmRegistration;

/// <summary>
/// Command to confirm a pending or waitlisted registration.
/// </summary>
public class ConfirmRegistrationCommand : IRequest<BaseResponse<EventRegistrationDto>>
{
    /// <summary>
    /// Gets or sets identifier of the registration to confirm.
    /// </summary>
    public Guid RegistrationId { get; set; }

    /// <summary>
    /// Gets or sets identity ID of the admin performing the confirmation.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;
}
