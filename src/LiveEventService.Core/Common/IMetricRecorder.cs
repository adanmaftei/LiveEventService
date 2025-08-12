namespace LiveEventService.Core.Common;

public interface IMetricRecorder
{
    void RecordCacheHit();
    void RecordCacheMiss();
    void RecordCacheSet();
}

public sealed class NoOpMetricRecorder : IMetricRecorder
{
    public void RecordCacheHit() {}
    public void RecordCacheMiss() {}
    public void RecordCacheSet() {}
}


