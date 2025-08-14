using HotChocolate.Authorization;
using LiveEventService.Application.Features.Events.EventRegistration.Get;
using LiveEventService.Core.Common;
using MediatR;

namespace LiveEventService.API.Events;

[ExtendObjectType(OperationTypeNames.Query)]
public class EventAdminQueries
{
    [Authorize(Roles = [RoleNames.Admin])]
    public async Task<string> ExportEventRegistrationsCsv(
        [Service] IMediator mediator,
        Guid eventId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEventRegistrationsQuery
        {
            EventId = eventId,
            PageNumber = 1,
            PageSize = int.MaxValue,
            Status = status
        };
        var result = await mediator.Send(query, cancellationToken);
        if (!result.Success || result.Data == null)
        {
            throw new GraphQLException(result.Errors?.FirstOrDefault() ?? "Error retrieving event registrations");
        }
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("RegistrationId,EventId,UserId,UserName,UserEmail,RegistrationDate,Status,PositionInQueue,Notes");
        foreach (var r in result.Data.Items)
        {
            var line = string.Join(',', new[]
            {
                r.Id.ToString(),
                r.EventId.ToString(),
                r.UserId.ToString(),
                Escape(r.UserName),
                Escape(r.UserEmail),
                r.RegistrationDate.ToString("o"),
                Escape(r.Status),
                r.PositionInQueue?.ToString() ?? string.Empty,
                Escape(r.Notes ?? string.Empty)
            });
            sb.AppendLine(line);
        }
        static string Escape(string input)
        {
            if (input.Contains('"') || input.Contains(',') || input.Contains('\n'))
            {
                return '"' + input.Replace("\"", "\"\"") + '"';
            }
            return input;
        }
        return sb.ToString();
    }
}


