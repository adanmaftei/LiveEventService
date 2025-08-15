using LiveEventService.Core.Common;
using LiveEventService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LiveEventService.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core tools, allowing migrations and scaffolding
/// without constructing the full application host.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LiveEventDbContext>
{
    /// <summary>
    /// Creates a DbContext instance configured for design-time operations.
    /// </summary>
    /// <param name="args">Command-line arguments (unused).</param>
    /// <returns>A configured <see cref="LiveEventDbContext"/>.</returns>
    public LiveEventDbContext CreateDbContext(string[] args)
    {
        // Build minimal configuration (prefer env vars; fall back to dev defaults)
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("src/LiveEventService.API/appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"]
            ?? "Host=localhost;Port=5432;Database=LiveEventDB;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<LiveEventDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            npgsqlOptions.CommandTimeout(30);
        });

        // Provide no-op dispatcher and pass-through encryption based on configuration
        var dispatcher = new NoOpDomainEventDispatcher();
        var encryption = new FieldEncryptionService(configuration);

        return new LiveEventDbContext(optionsBuilder.Options, dispatcher, encryption);
    }

    /// <summary>
    /// Minimal dispatcher used at design-time to satisfy the DbContext dependency.
    /// </summary>
    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAndClearEventsAsync(IEnumerable<Entity> entitiesWithEvents, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
