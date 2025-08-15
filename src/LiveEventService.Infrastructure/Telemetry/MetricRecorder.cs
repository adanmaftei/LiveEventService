using LiveEventService.Core.Common;

namespace LiveEventService.Infrastructure.Telemetry;

/// <summary>
/// Infrastructure-backed implementation of <see cref="IMetricRecorder"/> that forwards
/// calls to the counters defined in <see cref="AppMetrics"/>.
/// </summary>
public sealed class MetricRecorder : IMetricRecorder
{
    public void RecordCacheHit() => AppMetrics.CacheHits.Add(1);
    public void RecordCacheMiss() => AppMetrics.CacheMisses.Add(1);
    public void RecordCacheSet() => AppMetrics.CacheSets.Add(1);
    public void RecordEventCreated() => AppMetrics.EventsCreated.Add(1);
    public void RecordEventUpdated() => AppMetrics.EventsUpdated.Add(1);
    public void RecordEventDeleted() => AppMetrics.EventsDeleted.Add(1);
    public void RecordEventPublished() => AppMetrics.EventsPublished.Add(1);
    public void RecordEventUnpublished() => AppMetrics.EventsUnpublished.Add(1);
    public void RecordRegistrationCreated() => AppMetrics.RegistrationsCreated.Add(1);
    public void RecordRegistrationCancelled() => AppMetrics.RegistrationsCancelled.Add(1);
}
