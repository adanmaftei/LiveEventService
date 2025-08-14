using LiveEventService.Core.Common;
using LiveEventService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LiveEventService.Infrastructure.Data;

// Design-time factory for EF Core tools to avoid building the full host
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LiveEventDbContext>
{
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

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAndClearEventsAsync(IEnumerable<Entity> entitiesWithEvents, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}


