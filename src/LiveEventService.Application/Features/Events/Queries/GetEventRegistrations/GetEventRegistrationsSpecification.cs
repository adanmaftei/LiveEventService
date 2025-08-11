using LiveEventService.Core.Common;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Application.Features.Events.Queries.GetEventRegistrations;

public class GetEventRegistrationsSpecification : BaseSpecification<EventRegistrationEntity>
{
    public GetEventRegistrationsSpecification(Guid eventId, string? status, Guid? userId)
    {
        Criteria = er =>
            er.EventId == eventId &&
            (string.IsNullOrEmpty(status) || er.Status.ToString() == status) &&
            (!userId.HasValue || er.UserId == userId.Value);
            
        AddInclude(er => er.User);
        AddInclude(er => er.Event);
        ApplyOrderBy(er => er.RegistrationDate);
    }
} 