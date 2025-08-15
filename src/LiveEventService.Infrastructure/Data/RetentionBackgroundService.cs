using LiveEventService.Core.Registrations.EventRegistration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LiveEventService.Infrastructure.Data;

/// <summary>
/// Periodic background job that enforces data retention rules such as anonymizing
/// inactive users and removing old cancelled registrations.
/// </summary>
public sealed class RetentionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetentionBackgroundService> _logger;
    private readonly RetentionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionBackgroundService"/> class.
    /// Creates a new background service instance.
    /// </summary>
    /// <param name="serviceProvider">The service provider for creating scoped services.</param>
    /// <param name="logger">The logger for recording service events.</param>
    /// <param name="options">The retention configuration options.</param>
    public RetentionBackgroundService(IServiceProvider serviceProvider,
        ILogger<RetentionBackgroundService> logger,
        IOptions<RetentionOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Data retention service disabled");
            return;
        }

        _logger.LogInformation("Data retention service started");
        var delay = TimeSpan.FromHours(Math.Max(1, _options.IntervalHours));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data retention execution error");
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Data retention service stopped");
    }

    /// <summary>
    /// Executes one pass of the retention logic.
    /// </summary>
    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();

        var now = DateTime.UtcNow;

        // 1) Anonymize inactive users beyond retention window
        if (_options.UsersRetentionDays > 0)
        {
            var cutoff = now.AddDays(-_options.UsersRetentionDays);
            var users = await db.Users
                .IgnoreQueryFilters()
                .Where(u => !u.IsActive && u.UpdatedAt != null && u.UpdatedAt <= cutoff)
                .ToListAsync(ct);

            foreach (var u in users)
            {
                // Already anonymized by erase endpoint, but double-check
                if (!string.IsNullOrEmpty(u.Email) && !u.Email.StartsWith("anon+", StringComparison.OrdinalIgnoreCase))
                {
                    u.DeactivateAndAnonymize($"anon+{u.Id}@example.invalid");
                }
            }
            if (users.Count > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Retention: anonymized {Count} inactive users", users.Count);
            }
        }

        // 2) Delete cancelled registrations beyond retention window (safe)
        if (_options.CancelledRegistrationsRetentionDays > 0)
        {
            var cutoff = now.AddDays(-_options.CancelledRegistrationsRetentionDays);
            var oldCancelled = await db.EventRegistrations
                .IgnoreQueryFilters()
                .Where(r => r.Status == RegistrationStatus.Cancelled && r.RegistrationDate <= cutoff)
                .Select(r => r.Id)
                .ToListAsync(ct);

            if (oldCancelled.Count > 0)
            {
                var entities = await db.EventRegistrations.Where(r => oldCancelled.Contains(r.Id)).ToListAsync(ct);
                db.EventRegistrations.RemoveRange(entities);
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Retention: deleted {Count} cancelled registrations", entities.Count);
            }
        }
    }
}

/// <summary>
/// Configuration for data retention background job.
/// </summary>
public sealed class RetentionOptions
{
    public bool Enabled { get; set; } = false;
    public int IntervalHours { get; set; } = 24;
    public int UsersRetentionDays { get; set; } = 0;
    public int CancelledRegistrationsRetentionDays { get; set; } = 0;
}
