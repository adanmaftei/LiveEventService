namespace LiveEventService.Core.Common;

/// <summary>
/// Records application and domain metrics.
/// </summary>
public interface IMetricRecorder
{
    /// <summary>Records a cache hit event.</summary>
    void RecordCacheHit();

    /// <summary>Records a cache miss event.</summary>
    void RecordCacheMiss();

    /// <summary>Records a cache set operation.</summary>
    void RecordCacheSet();
    // Domain metrics

    /// <summary>Records that an event was created.</summary>
    void RecordEventCreated();

    /// <summary>Records that an event was updated.</summary>
    void RecordEventUpdated();

    /// <summary>Records that an event was deleted.</summary>
    void RecordEventDeleted();

    /// <summary>Records that an event was published.</summary>
    void RecordEventPublished();

    /// <summary>Records that an event was unpublished.</summary>
    void RecordEventUnpublished();

    /// <summary>Records that a registration was created.</summary>
    void RecordRegistrationCreated();

    /// <summary>Records that a registration was cancelled.</summary>
    void RecordRegistrationCancelled();
}

/// <summary>
/// No-op implementation of <see cref="IMetricRecorder"/> used when metrics are disabled.
/// </summary>
public sealed class NoOpMetricRecorder : IMetricRecorder
{
    public void RecordCacheHit() { }
    public void RecordCacheMiss() { }
    public void RecordCacheSet() { }
    public void RecordEventCreated() { }
    public void RecordEventUpdated() { }
    public void RecordEventDeleted() { }
    public void RecordEventPublished() { }
    public void RecordEventUnpublished() { }
    public void RecordRegistrationCreated() { }
    public void RecordRegistrationCancelled() { }
}
