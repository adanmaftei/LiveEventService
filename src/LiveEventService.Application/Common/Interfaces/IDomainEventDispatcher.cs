using LiveEventService.Core.Common;

namespace LiveEventService.Application.Common.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAndClearEventsAsync(IEnumerable<Entity> entities, CancellationToken cancellationToken = default);
} 