using LiveEventService.Core.Common;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Application.Features.Events.Queries.GetEventRegistrations;

/// <summary>
/// Specification to filter, include related entities, and order event registrations.
/// </summary>
public class GetEventRegistrationsSpecification : BaseSpecification<EventRegistrationEntity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetEventRegistrationsSpecification"/> class.
    /// Creates a specification for registrations for an event with optional status and user filters.
    /// </summary>
    /// <param name="eventId">The event ID to filter registrations by.</param>
    /// <param name="status">Optional registration status to filter by.</param>
    /// <param name="userId">Optional user ID to filter registrations by.</param>
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
