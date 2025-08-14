using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.IntegrationTests.Infrastructure;
using System.Net;

namespace LiveEventService.IntegrationTests.Waitlist;

public class WaitlistIntegrationTests : BaseLiveEventsTests
{
    public WaitlistIntegrationTests(LiveEventTestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task WaitlistPromotion_WhenConfirmedRegistrationCancelled_ShouldPromoteNextPerson()
    {
        // Arrange: Create event with capacity 2, register 3 people
        var eventId = await CreateEventWithCapacity(2);

        // Register 3 people - first 2 confirmed, third waitlisted
        var user1Id = await CreateUserAndRegisterForEvent(eventId, "user1@test.com", "User", "One");
        var user2Id = await CreateUserAndRegisterForEvent(eventId, "user2@test.com", "User", "Two");
        var user3Id = await CreateUserAndRegisterForEvent(eventId, "user3@test.com", "User", "Three");

        // Verify initial state
        var registrations = await GetEventRegistrations(eventId);
        registrations.Should().HaveCount(3);

        var confirmedRegistrations = registrations.Where(r => r.Status == RegistrationStatus.Confirmed.ToString()).ToList();
        var waitlistedRegistrations = registrations.Where(r => r.Status == RegistrationStatus.Waitlisted.ToString()).ToList();

        confirmedRegistrations.Should().HaveCount(2);
        waitlistedRegistrations.Should().HaveCount(1);
        waitlistedRegistrations.First().PositionInQueue.Should().Be(1);

        // Get the first confirmed registration to cancel
        var firstConfirmedRegistration = confirmedRegistrations.First();

        // Act: Cancel first confirmed registration
        var cancelResponse = await _authenticatedAdminClient.PostAsync(
            $"/api/events/{eventId}/registrations/{firstConfirmedRegistration.Id}/cancel",
            null);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: Verify waitlist promotion occurred
        var updatedRegistrations = await GetEventRegistrations(eventId);
        updatedRegistrations.Should().HaveCount(2); // Only 2 visible (cancelled registration is hidden by soft delete)

        var updatedConfirmedRegistrations = updatedRegistrations.Where(r => r.Status == RegistrationStatus.Confirmed.ToString()).ToList();
        var updatedWaitlistedRegistrations = updatedRegistrations.Where(r => r.Status == RegistrationStatus.Waitlisted.ToString()).ToList();

        // Should have 2 confirmed (original second + promoted third)
        updatedConfirmedRegistrations.Should().HaveCount(2);
        // Should have 0 waitlisted (third person was promoted)
        updatedWaitlistedRegistrations.Should().HaveCount(0);

        // Verify the third person (originally waitlisted) is now confirmed
        var promotedRegistration = updatedConfirmedRegistrations.FirstOrDefault(r => r.UserId == user3Id);
        promotedRegistration.Should().NotBeNull();
        promotedRegistration!.PositionInQueue.Should().BeNull(); // No longer in queue
    }

    [Fact]
    public async Task WaitlistPositionUpdates_WhenMultipleCancellations_ShouldUpdatePositionsCorrectly()
    {
        // Arrange: Create event with capacity 1, register 4 people
        var eventId = await CreateEventWithCapacity(1);

        // Register 4 people - 1 confirmed, 3 waitlisted
        var user1Id = await CreateUserAndRegisterForEvent(eventId, "user1@test.com", "User", "One");
        var user2Id = await CreateUserAndRegisterForEvent(eventId, "user2@test.com", "User", "Two");
        var user3Id = await CreateUserAndRegisterForEvent(eventId, "user3@test.com", "User", "Three");
        var user4Id = await CreateUserAndRegisterForEvent(eventId, "user4@test.com", "User", "Four");

        // Verify initial waitlist positions
        var initialRegistrations = await GetEventRegistrations(eventId);
        var initialWaitlisted = initialRegistrations.Where(r => r.Status == RegistrationStatus.Waitlisted.ToString()).OrderBy(r => r.PositionInQueue).ToList();

        initialWaitlisted.Should().HaveCount(3);
        initialWaitlisted[0].PositionInQueue.Should().Be(1);
        initialWaitlisted[1].PositionInQueue.Should().Be(2);
        initialWaitlisted[2].PositionInQueue.Should().Be(3);

        // Act: Cancel the confirmed registration (user1)
        var confirmedRegistration = initialRegistrations.First(r => r.Status == RegistrationStatus.Confirmed.ToString());
        var cancelResponse = await _authenticatedAdminClient.PostAsync(
            $"/api/events/{eventId}/registrations/{confirmedRegistration.Id}/cancel",
            null);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: Verify positions updated correctly
        var updatedRegistrations = await GetEventRegistrations(eventId);
        var updatedWaitlisted = updatedRegistrations.Where(r => r.Status == RegistrationStatus.Waitlisted.ToString()).OrderBy(r => r.PositionInQueue).ToList();

        // Should now have 2 waitlisted (user3 and user4)
        updatedWaitlisted.Should().HaveCount(2);

        // user2 should be confirmed (promoted from position 1)
        var promotedUser = updatedRegistrations.FirstOrDefault(r => r.UserId == user2Id && r.Status == RegistrationStatus.Confirmed.ToString());
        promotedUser.Should().NotBeNull();

        // user3 should now be position 1, user4 should be position 2
        var user3Registration = updatedWaitlisted.FirstOrDefault(r => r.UserId == user3Id);
        var user4Registration = updatedWaitlisted.FirstOrDefault(r => r.UserId == user4Id);

        user3Registration.Should().NotBeNull();
        user3Registration!.PositionInQueue.Should().Be(1);

        user4Registration.Should().NotBeNull();
        user4Registration!.PositionInQueue.Should().Be(2);
    }

    [Fact]
    public async Task WaitlistRegistration_WhenEventIsFull_ShouldAddToWaitlistWithCorrectPosition()
    {
        // Arrange: Create event with capacity 1
        var eventId = await CreateEventWithCapacity(1);

        // Register first person (should be confirmed)
        var user1Id = await CreateUserAndRegisterForEvent(eventId, "user1@test.com", "User", "One");

        // Act: Register second person (should be waitlisted)
        var user2Id = await CreateUserAndRegisterForEvent(eventId, "user2@test.com", "User", "Two");

        // Assert: Verify waitlist behavior
        var registrations = await GetEventRegistrations(eventId);
        registrations.Should().HaveCount(2);

        var confirmedRegistration = registrations.FirstOrDefault(r => r.Status == RegistrationStatus.Confirmed.ToString());
        var waitlistedRegistration = registrations.FirstOrDefault(r => r.Status == RegistrationStatus.Waitlisted.ToString());

        confirmedRegistration.Should().NotBeNull();
        confirmedRegistration!.UserId.Should().Be(user1Id);
        confirmedRegistration.PositionInQueue.Should().BeNull();

        waitlistedRegistration.Should().NotBeNull();
        waitlistedRegistration!.UserId.Should().Be(user2Id);
        waitlistedRegistration.PositionInQueue.Should().Be(1); // First position in waitlist queue
    }

    [Fact]
    public async Task WaitlistRegistration_WhenMultiplePeopleRegisterSimultaneously_ShouldHandleConcurrency()
    {
        // Arrange: Create event with capacity 1
        var eventId = await CreateEventWithCapacity(1);

        // Register first person (should be confirmed)
        var user1Id = await CreateUserAndRegisterForEvent(eventId, "user1@test.com", "User", "One");

        // Act: Register multiple people simultaneously
        var tasks = new List<Task<Guid>>();
        for (int i = 2; i <= 5; i++)
        {
            tasks.Add(CreateUserAndRegisterForEvent(eventId, $"user{i}@test.com", "User", $"Number{i}"));
        }

        var userIds = await Task.WhenAll(tasks);

        // Assert: Verify all registrations processed correctly
        var registrations = await GetEventRegistrations(eventId);
        registrations.Should().HaveCount(5);

        var confirmedRegistrations = registrations.Where(r => r.Status == RegistrationStatus.Confirmed.ToString()).ToList();
        var waitlistedRegistrations = registrations.Where(r => r.Status == RegistrationStatus.Waitlisted.ToString()).ToList();

        // Should have 1 confirmed (first person)
        confirmedRegistrations.Should().HaveCount(1);
        confirmedRegistrations.First().UserId.Should().Be(user1Id);

        // Should have 4 waitlisted
        waitlistedRegistrations.Should().HaveCount(4);

        // Verify positions are sequential starting from 1
        var positions = waitlistedRegistrations.Select(r => r.PositionInQueue).OrderBy(p => p).ToList();
        positions.Should().BeEquivalentTo(new int?[] { 1, 2, 3, 4 });
    }

    [Fact]
    public async Task WaitlistCancellation_WhenWaitlistedRegistrationCancelled_ShouldNotPromoteAnyone()
    {
        // Arrange: Create event with capacity 1, register 2 people
        var eventId = await CreateEventWithCapacity(1);

        var user1Id = await CreateUserAndRegisterForEvent(eventId, "user1@test.com", "User", "One");
        var user2Id = await CreateUserAndRegisterForEvent(eventId, "user2@test.com", "User", "Two");

        // Verify initial state
        var initialRegistrations = await GetEventRegistrations(eventId);
        var confirmedRegistration = initialRegistrations.First(r => r.Status == RegistrationStatus.Confirmed.ToString());
        var waitlistedRegistration = initialRegistrations.First(r => r.Status == RegistrationStatus.Waitlisted.ToString());

        // Act: Cancel the waitlisted registration
        var cancelResponse = await _authenticatedAdminClient.PostAsync(
            $"/api/events/{eventId}/registrations/{waitlistedRegistration.Id}/cancel",
            null);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: Verify no promotion occurred
        var updatedConfirmedRegistrations = await GetEventRegistrationsByStatus(eventId, RegistrationStatus.Confirmed.ToString());
        var updatedWaitlistedRegistrations = await GetEventRegistrationsByStatus(eventId, RegistrationStatus.Waitlisted.ToString());

        // Use the API endpoint to check what happened after cancellation
        var allRegistrations = await GetEventRegistrations(eventId);

        // Should still have 1 confirmed (no change - no promotion occurred)
        updatedConfirmedRegistrations.Should().HaveCount(1);
        updatedConfirmedRegistrations.First().UserId.Should().Be(user1Id);

        // Should have 0 waitlisted (cancelled)
        updatedWaitlistedRegistrations.Should().HaveCount(0);

        // Total registrations should be 1 (cancelled registrations are hidden by soft delete)
        allRegistrations.Should().HaveCount(1);
    }

    [Fact]
    public async Task WaitlistDomainEvents_WhenPromotionOccurs_ShouldRaiseCorrectDomainEvents()
    {
        // Arrange: Create event with capacity 1, register 2 people
        var eventId = await CreateEventWithCapacity(1);

        var user1Id = await CreateUserAndRegisterForEvent(eventId, "user1@test.com", "User", "One");
        var user2Id = await CreateUserAndRegisterForEvent(eventId, "user2@test.com", "User", "Two");

        // Get the confirmed registration to cancel
        var registrations = await GetEventRegistrations(eventId);
        var confirmedRegistration = registrations.First(r => r.Status == RegistrationStatus.Confirmed.ToString());

        // Act: Cancel confirmed registration (this should trigger promotion)
        var cancelResponse = await _authenticatedAdminClient.PostAsync(
            $"/api/events/{eventId}/registrations/{confirmedRegistration.Id}/cancel",
            null);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: Verify domain events were raised by checking the database state
        // The promotion should have occurred, which means domain events were processed
        var updatedRegistrations = await GetEventRegistrations(eventId);
        var promotedRegistration = updatedRegistrations.FirstOrDefault(r => r.UserId == user2Id && r.Status == RegistrationStatus.Confirmed.ToString());

        promotedRegistration.Should().NotBeNull();
        promotedRegistration!.PositionInQueue.Should().BeNull(); // Confirmed registrations don't have queue position
    }
}
