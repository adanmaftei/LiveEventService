using LiveEventService.Core.Common;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Application.Features.Events.Queries.GetEventRegistrations;

/// <summary>
/// Optimized specification for counting registrations without including related entities.
/// </summary>
public class GetEventRegistrationsCountSpecification : BaseSpecification<EventRegistrationEntity>
{
    public GetEventRegistrationsCountSpecification(Guid eventId, string? status, Guid? userId)
    {
        Criteria = er =>
            er.EventId == eventId &&
            (string.IsNullOrEmpty(status) || er.Status.ToString() == status) &&
            (!userId.HasValue || er.UserId == userId.Value);

        // No includes for count operations - much faster
        ApplyOrderBy(er => er.RegistrationDate);
    }
}
