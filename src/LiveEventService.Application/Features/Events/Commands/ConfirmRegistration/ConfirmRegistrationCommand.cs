using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.EventRegistration;
using MediatR;

namespace LiveEventService.Application.Features.Events.Commands.ConfirmRegistration;

public class ConfirmRegistrationCommand : IRequest<BaseResponse<EventRegistrationDto>>
{
    public Guid RegistrationId { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
}
