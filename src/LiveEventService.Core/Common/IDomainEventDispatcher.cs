using LiveEventService.Core.Common;

namespace LiveEventService.Core.Common;

public interface IDomainEventDispatcher
{
    Task DispatchAndClearEventsAsync(IEnumerable<Entity> entities, CancellationToken cancellationToken = default);
}