using LiveEventService.Core.Common;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Application.Features.Events.Queries.GetEventRegistrations;

public class GetEventRegistrationsSpecification : BaseSpecification<EventRegistrationEntity>
{
    public GetEventRegistrationsSpecification(Guid eventId, string? status, Guid? userId)
    {
        Criteria = er =>
            er.EventId == eventId &&
            (status == null || status == string.Empty || er.Status == Enum.Parse<Core.Registrations.EventRegistration.RegistrationStatus>(status)) &&
            (!userId.HasValue || er.UserId == userId.Value);

        AddInclude(er => er.User);
        AddInclude(er => er.Event);
        ApplyOrderBy(er => er.RegistrationDate);
    }
}
