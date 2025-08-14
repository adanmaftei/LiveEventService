using LiveEventService.Core.Common;

namespace LiveEventService.Core.Events;

public interface IEventRepository : IRepository<Event>
{
    Task<bool> IsUserRegisteredForEventAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetRegistrationCountForEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<int> GetWaitlistCountForEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<int> CalculateWaitlistPositionAsync(Guid eventId, Guid registrationId, CancellationToken cancellationToken = default);
}
