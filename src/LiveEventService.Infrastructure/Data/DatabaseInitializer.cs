using LiveEventService.Core.Events;
using UserEntity = LiveEventService.Core.Users.User.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LiveEventService.Infrastructure.Data;

/// <summary>
/// Provides helper to apply pending EF Core migrations and seed basic test data
/// in development environments. Intended for local/demo scenarios.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations and seeds a minimal set of users and events if the database is empty.
    /// </summary>
    /// <param name="services">Root service provider to resolve scoped services.</param>
    /// <returns>A task that completes when initialization has finished.</returns>
    public static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var logger = scopedServices.GetRequiredService<ILogger<LiveEventDbContext>>();

        try
        {
            var context = scopedServices.GetRequiredService<LiveEventDbContext>();

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync();
            }

            // Check if we already have data
            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Database already contains data. Skipping test data initialization.");
                return;
            }

            logger.LogInformation("Seeding test data...");

            // Create test users
            var adminUser = new UserEntity(
                identityId: "auth0|testadmin1",
                email: "admin1@example.com",
                firstName: "John",
                lastName: "Admin",
                phoneNumber: "+1234567890");

            var regularUser1 = new UserEntity(
                identityId: "auth0|testuser1",
                email: "user1@example.com",
                firstName: "Alice",
                lastName: "Attendee",
                phoneNumber: "+1122334455");

            var regularUser2 = new UserEntity(
                identityId: "auth0|testuser2",
                email: "user2@example.com",
                firstName: "Bob",
                lastName: "Participant",
                phoneNumber: "+1555666777");

            context.Users.AddRange(adminUser, regularUser1, regularUser2);

            // Create test events (admin is the organizer for both)
            var now = DateTime.UtcNow;

            var event1 = new Event(
                "Tech Conference 2023",
                "Annual technology conference with workshops and keynotes",
                now.AddDays(30),
                now.AddDays(33),
                1000,
                "America/New_York",
                "New York Convention Center",
                adminUser.IdentityId);

            var event2 = new Event(
                "Music Festival",
                "Weekend music festival featuring various artists",
                now.AddDays(45),
                now.AddDays(47),
                5000,
                "America/Los_Angeles",
                "Sunset Park",
                adminUser.IdentityId);

            context.Events.AddRange(event1, event2);

            await context.SaveChangesAsync();

            logger.LogInformation("Test data seeded successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }
}
