using HotChocolate.Authorization;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Application.Features.Events.Event.Create;
using LiveEventService.Application.Features.Events.Event.Delete;
using LiveEventService.Application.Features.Events.Event.Update;
using LiveEventService.Application.Features.Events.EventRegistration;
using LiveEventService.Application.Features.Events.EventRegistration.Register;
using LiveEventService.Core.Common;
using MediatR;

namespace LiveEventService.API.Events;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class EventMutations
{
    [Authorize(Roles = [RoleNames.Admin])]
    public async Task<EventDto> CreateEvent(
        [Service] IMediator mediator,
        CreateEventInput input,
        [GlobalState] string currentUserId,
        CancellationToken cancellationToken)
    {
        var command = new CreateEventCommand
        {
            OrganizerId = currentUserId,
            Event = input
        };

        var result = await mediator.Send(command, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error creating event");
        }

        return result.Data;
    }

    [Authorize(Roles = [RoleNames.Admin])]
    public async Task<EventDto> UpdateEvent(
        [Service] IMediator mediator,
        UpdateEventInput input,
        [GlobalState] string currentUserId,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEventCommand
        {
            EventId = input.Id,
            UserId = currentUserId,
            Event = input
        };

        var result = await mediator.Send(command, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error updating event");
        }

        return result.Data;
    }

    [Authorize(Roles = [RoleNames.Admin])]
    public async Task<bool> DeleteEvent(
        [Service] IMediator mediator,
        Guid eventId,
        [GlobalState] string currentUserId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteEventCommand
        {
            EventId = eventId,
            UserId = currentUserId
        };

        var result = await mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error deleting event");
        }

        return true;
    }

    [Authorize]
    public async Task<EventRegistrationDto> RegisterForEvent(
        [Service] IMediator mediator,
        RegisterForEventInput input,
        [GlobalState] string currentUserId,
        CancellationToken cancellationToken)
    {
        var command = new RegisterForEventCommand
        {
            EventId = input.EventId,
            UserId = currentUserId,
            Notes = input.Notes
        };

        var result = await mediator.Send(command, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error registering for event");
        }

        return result.Data;
    }
}

public class CreateEventInput : CreateEventDto { }
public class UpdateEventInput : UpdateEventDto { }
public class RegisterForEventInput
{
    public Guid EventId { get; set; }
    public string? Notes { get; set; }
}
