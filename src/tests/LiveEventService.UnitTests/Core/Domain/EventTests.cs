using LiveEventService.Core.Events;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Users.User;
using LiveEventService.UnitTests.Common;

namespace LiveEventService.UnitTests.Core.Domain;

public class EventTests : TestBase
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateEvent()
    {
        // Arrange
        var name = "Test Event";
        var description = "Test Description";
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = startDate.AddHours(2);
        var capacity = 100;
        var timeZone = "UTC";
        var location = "Test Location";
        var organizerId = "organizer-123";

        // Act
        var @event = new Event(name, description, startDate, endDate, capacity, timeZone, location, organizerId);

        // Assert
        AssertNotNull(@event);
        AssertEqual(name, @event.Name);
        AssertEqual(description, @event.Description);
        AssertEqual(startDate, @event.StartDate);
        AssertEqual(endDate, @event.EndDate);
        AssertEqual(capacity, @event.Capacity);
        AssertEqual(timeZone, @event.TimeZone);
        AssertEqual(location, @event.Location);
        AssertEqual(organizerId, @event.OrganizerId);
        AssertFalse(@event.IsPublished);
        AssertCollectionEmpty(@event.Registrations);
    }

    [Fact]
    public void Constructor_ShouldSetIdAndTimestamps()
    {
        // Arrange & Act
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");

        // Assert
        AssertNotEqual(Guid.Empty, @event.Id);
        AssertTrue(@event.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
        AssertNull(@event.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateEvent()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var newName = "Updated Event";
        var newDescription = "Updated Description";
        var newStartDate = DateTime.UtcNow.AddDays(2);
        var newEndDate = newStartDate.AddHours(3);
        var newCapacity = 200;
        var newTimeZone = "America/New_York";
        var newLocation = "Updated Location";

        // Act
        @event.UpdateDetails(newName, newDescription, newStartDate, newEndDate, newCapacity, newTimeZone, newLocation);

        // Assert
        AssertEqual(newName, @event.Name);
        AssertEqual(newDescription, @event.Description);
        AssertEqual(newStartDate, @event.StartDate);
        AssertEqual(newEndDate, @event.EndDate);
        AssertEqual(newCapacity, @event.Capacity);
        AssertEqual(newTimeZone, @event.TimeZone);
        AssertEqual(newLocation, @event.Location);
    }

    [Fact]
    public void Publish_ShouldSetIsPublishedToTrue()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        AssertFalse(@event.IsPublished);

        // Act
        @event.Publish();

        // Assert
        AssertTrue(@event.IsPublished);
    }

    [Fact]
    public void Unpublish_ShouldSetIsPublishedToFalse()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        @event.Publish();
        AssertTrue(@event.IsPublished);

        // Act
        @event.Unpublish();

        // Assert
        AssertFalse(@event.IsPublished);
    }

    [Fact]
    public void IsFull_WhenNoRegistrations_ShouldReturnFalse()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");

        // Act & Assert
        AssertFalse(@event.IsFull());
    }

    [Fact]
    public void IsFull_WhenRegistrationsBelowCapacity_ShouldReturnFalse()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        @event.AddRegistration(registration);

        // Act & Assert
        AssertFalse(@event.IsFull());
    }

    [Fact]
    public void IsFull_WhenRegistrationsAtCapacity_ShouldReturnTrue()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);

        // Act & Assert
        AssertTrue(@event.IsFull());
    }

    [Fact]
    public void IsFull_WhenRegistrationsAboveCapacity_ShouldReturnTrue()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        var registration1 = new EventRegistration(@event, user1);
        var registration2 = new EventRegistration(@event, user2);
        @event.AddRegistration(registration1);
        @event.AddRegistration(registration2);

        // Act & Assert
        AssertTrue(@event.IsFull());
    }

    [Fact]
    public void IsFull_WithCancelledRegistration_ShouldNotCountCancelled()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 2, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        var registration1 = new EventRegistration(@event, user1);
        var registration2 = new EventRegistration(@event, user2);
        @event.AddRegistration(registration1);
        @event.AddRegistration(registration2);

        // Confirm both registrations first (since IsFull only counts Confirmed registrations)
        registration1.Confirm();
        registration2.Confirm();

        registration2.Cancel(); // Cancel one registration

        // Act & Assert
        AssertFalse(@event.IsFull()); // Should not be full because the confirmed registration is cancelled
    }

    [Fact]
    public void AddRegistration_ShouldAddToRegistrations()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);

        // Act
        @event.AddRegistration(registration);

        // Assert
        AssertCollectionCount(@event.Registrations, 1);
        AssertTrue(@event.Registrations.Contains(registration));
    }

    [Fact]
    public void RemoveRegistration_ShouldRemoveFromRegistrations()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        @event.AddRegistration(registration);
        AssertCollectionCount(@event.Registrations, 1);

        // Act
        @event.RemoveRegistration(registration);

        // Assert
        AssertCollectionEmpty(@event.Registrations);
    }

    [Fact]
    public void Constructor_WithInvalidData_ShouldStillCreateEvent()
    {
        // Arrange
        var name = ""; // Invalid name
        var description = "Test Description";
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = startDate.AddHours(2);
        var capacity = 0; // Invalid capacity
        var timeZone = "UTC";
        var location = "Test Location";
        var organizerId = "organizer-123";

        // Act & Assert - Should not throw, just create with invalid data
        var @event = new Event(name, description, startDate, endDate, capacity, timeZone, location, organizerId);
        AssertNotNull(@event);
        AssertEqual(name, @event.Name);
        AssertEqual(capacity, @event.Capacity);
    }

    [Fact]
    public void Constructor_WithEndDateBeforeStartDate_ShouldStillCreateEvent()
    {
        // Arrange
        var name = "Test Event";
        var description = "Test Description";
        var startDate = DateTime.UtcNow.AddDays(2);
        var endDate = DateTime.UtcNow.AddDays(1); // End before start
        var capacity = 100;
        var timeZone = "UTC";
        var location = "Test Location";
        var organizerId = "organizer-123";

        // Act & Assert - Should not throw, just create with invalid dates
        var @event = new Event(name, description, startDate, endDate, capacity, timeZone, location, organizerId);
        AssertNotNull(@event);
        AssertEqual(startDate, @event.StartDate);
        AssertEqual(endDate, @event.EndDate);
    }
}
