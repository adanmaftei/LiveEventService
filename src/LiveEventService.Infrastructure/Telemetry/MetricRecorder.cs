using LiveEventService.Core.Common;

namespace LiveEventService.Infrastructure.Telemetry;

public sealed class MetricRecorder : IMetricRecorder
{
    public void RecordCacheHit() => AppMetrics.CacheHits.Add(1);
    public void RecordCacheMiss() => AppMetrics.CacheMisses.Add(1);
    public void RecordCacheSet() => AppMetrics.CacheSets.Add(1);
}


