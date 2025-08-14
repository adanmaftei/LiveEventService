using System.Diagnostics.Metrics;

namespace LiveEventService.Infrastructure.Telemetry;

public static class AppMetrics
{
    public const string MeterName = "LiveEventService.Metrics";
    private static readonly Meter s_meter = new(MeterName);

    public static readonly Counter<long> EventsCreated = s_meter.CreateCounter<long>(
        name: "events_created_total",
        unit: "count",
        description: "Number of events created successfully");

    public static readonly Counter<long> EventsUpdated = s_meter.CreateCounter<long>(
        name: "events_updated_total",
        unit: "count",
        description: "Number of events updated successfully");

    public static readonly Counter<long> EventsDeleted = s_meter.CreateCounter<long>(
        name: "events_deleted_total",
        unit: "count",
        description: "Number of events deleted successfully");

    public static readonly Counter<long> EventsPublished = s_meter.CreateCounter<long>(
        name: "events_published_total",
        unit: "count",
        description: "Number of events published successfully");

    public static readonly Counter<long> EventsUnpublished = s_meter.CreateCounter<long>(
        name: "events_unpublished_total",
        unit: "count",
        description: "Number of events unpublished successfully");

    public static readonly Counter<long> RegistrationsCreated = s_meter.CreateCounter<long>(
        name: "registrations_created_total",
        unit: "count",
        description: "Number of registrations created successfully");

    public static readonly Counter<long> RegistrationsCancelled = s_meter.CreateCounter<long>(
        name: "registrations_cancelled_total",
        unit: "count",
        description: "Number of registrations cancelled successfully");

    public static readonly Counter<long> OutboxProcessed = s_meter.CreateCounter<long>(
        name: "outbox_processed_total",
        unit: "count",
        description: "Number of outbox messages processed successfully");

    public static readonly Counter<long> OutboxFailed = s_meter.CreateCounter<long>(
        name: "outbox_failed_total",
        unit: "count",
        description: "Number of outbox messages that failed and were rescheduled");

    // Outbox pending: use an observable gauge to report the latest value
    private static long s_outboxPending;
    private static readonly ObservableGauge<long> s_outboxPendingGauge = s_meter.CreateObservableGauge(
        name: "outbox_pending_count",
        observeValue: () => Interlocked.Read(ref s_outboxPending),
        unit: "count",
        description: "Current number of pending outbox messages");
    public static void SetOutboxPending(long pending)
    {
        Interlocked.Exchange(ref s_outboxPending, pending);
    }

    public static readonly Counter<long> CacheHits = s_meter.CreateCounter<long>(
        name: "cache_hits_total",
        unit: "count",
        description: "Number of cache hits from IDistributedCache");

    public static readonly Counter<long> CacheMisses = s_meter.CreateCounter<long>(
        name: "cache_misses_total",
        unit: "count",
        description: "Number of cache misses from IDistributedCache");

    public static readonly Counter<long> CacheSets = s_meter.CreateCounter<long>(
        name: "cache_sets_total",
        unit: "count",
        description: "Number of cache set operations performed");

    // Redis connectivity gauge (0 = disconnected, 1 = connected)
    private static Func<int>? s_redisConnectivityProvider;
    private static readonly ObservableGauge<int> s_redisConnectivity = s_meter.CreateObservableGauge(
        name: "redis_connected",
        observeValue: () => s_redisConnectivityProvider?.Invoke() ?? 0,
        unit: "state",
        description: "Redis connectivity: 1 when connected, 0 when not");

    public static void SetRedisConnectivityProvider(Func<int> provider)
    {
        s_redisConnectivityProvider = provider;
    }
}


