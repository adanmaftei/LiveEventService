using LiveEventService.Core.Events;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Users.User;
using LiveEventService.UnitTests.Common;

namespace LiveEventService.UnitTests.Core.Domain;

public class EventRegistrationTests : TestBase
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateRegistration()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var notes = "Test notes";

        // Act
        var registration = new EventRegistration(@event, user, notes);

        // Assert
        AssertNotNull(registration);
        AssertEqual(@event.Id, registration.EventId);
        AssertEqual(user.Id, registration.UserId);
        AssertEqual(@event, registration.Event);
        AssertEqual(user, registration.User);
        AssertEqual(notes, registration.Notes);
        AssertEqual(RegistrationStatus.Confirmed, registration.Status); // Should be confirmed since event is not full
        AssertTrue(registration.RegistrationDate > DateTime.UtcNow.AddMinutes(-1));
        AssertNull(registration.PositionInQueue);
        AssertCollectionCount(registration.DomainEvents, 1);
        AssertTrue(registration.DomainEvents.First() is EventRegistrationCreatedDomainEvent);
    }

    [Fact]
    public void Constructor_WhenEventIsFull_ShouldSetStatusToWaitlisted()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        // First registration fills the event
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        // Second registration should be waitlisted
        var registration2 = new EventRegistration(@event, user2);

        // Assert
        AssertEqual(RegistrationStatus.Waitlisted, registration2.Status);
        AssertCollectionCount(registration2.DomainEvents, 1);
        AssertTrue(registration2.DomainEvents.First() is EventRegistrationCreatedDomainEvent);
    }

    [Fact]
    public void Constructor_ShouldSetIdAndTimestamps()
    {
        // Arrange & Act
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);

        // Assert
        AssertNotEqual(Guid.Empty, registration.Id);
        AssertTrue(registration.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
        AssertNull(registration.UpdatedAt);
    }

    [Fact]
    public void Confirm_WhenPending_ShouldSetStatusToConfirmed()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        // Add first registration to make event full
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        // Create second registration (will be waitlisted)
        var registration = new EventRegistration(@event, user2);
        registration.ClearDomainEvents(); // Clear initial event

        // Act
        registration.Confirm();

        // Assert
        AssertEqual(RegistrationStatus.Confirmed, registration.Status);
        AssertNull(registration.PositionInQueue);
        AssertNotNull(registration.UpdatedAt);
        AssertCollectionCount(registration.DomainEvents, 1); // Should raise EventRegistrationPromotedDomainEvent
        AssertTrue(registration.DomainEvents.First() is EventRegistrationPromotedDomainEvent);
    }

    [Fact]
    public void Confirm_WhenWaitlisted_ShouldSetStatusToConfirmedAndRaisePromotedEvent()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        registration2.ClearDomainEvents(); // Clear initial event
        
        AssertEqual(RegistrationStatus.Waitlisted, registration2.Status);

        // Act
        registration2.Confirm();

        // Assert
        AssertEqual(RegistrationStatus.Confirmed, registration2.Status);
        AssertNull(registration2.PositionInQueue);
        AssertNotNull(registration2.UpdatedAt);
        AssertCollectionCount(registration2.DomainEvents, 1);
        AssertTrue(registration2.DomainEvents.First() is EventRegistrationPromotedDomainEvent);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldDoNothing()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        registration.ClearDomainEvents();
        var originalUpdatedAt = registration.UpdatedAt;

        // Act
        registration.Confirm();

        // Assert
        AssertEqual(RegistrationStatus.Confirmed, registration.Status);
        AssertEqual(originalUpdatedAt, registration.UpdatedAt);
        AssertCollectionEmpty(registration.DomainEvents);
    }

    [Fact]
    public void Confirm_WhenCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        registration.Cancel();
        registration.ClearDomainEvents();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => registration.Confirm());
        AssertTrue(exception.Message.Contains("Only pending or waitlisted registrations can be confirmed"));
    }

    [Fact]
    public void Cancel_WhenConfirmed_ShouldSetStatusToCancelledAndRaiseEvent()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        registration.ClearDomainEvents();

        // Act
        registration.Cancel();

        // Assert
        AssertEqual(RegistrationStatus.Cancelled, registration.Status);
        AssertNull(registration.PositionInQueue);
        AssertNotNull(registration.UpdatedAt);
        AssertCollectionCount(registration.DomainEvents, 1);
        AssertTrue(registration.DomainEvents.First() is EventRegistrationCancelledDomainEvent);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldDoNothing()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        registration.Cancel();
        registration.ClearDomainEvents();
        var originalUpdatedAt = registration.UpdatedAt;

        // Act
        registration.Cancel();

        // Assert
        AssertEqual(RegistrationStatus.Cancelled, registration.Status);
        AssertEqual(originalUpdatedAt, registration.UpdatedAt);
        AssertCollectionEmpty(registration.DomainEvents);
    }

    [Fact]
    public void MarkAsAttended_WhenConfirmed_ShouldSetStatusToAttended()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        registration.ClearDomainEvents();

        // Act
        registration.MarkAsAttended();

        // Assert
        AssertEqual(RegistrationStatus.Attended, registration.Status);
        AssertNotNull(registration.UpdatedAt);
    }

    [Fact]
    public void MarkAsAttended_WhenNotConfirmed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        registration.Cancel(); // Set to cancelled
        registration.ClearDomainEvents();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => registration.MarkAsAttended());
        AssertTrue(exception.Message.Contains("Only confirmed registrations can be marked as attended"));
    }

    [Fact]
    public void MarkAsNoShow_WhenConfirmed_ShouldSetStatusToNoShow()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        registration.ClearDomainEvents();

        // Act
        registration.MarkAsNoShow();

        // Assert
        AssertEqual(RegistrationStatus.NoShow, registration.Status);
        AssertNotNull(registration.UpdatedAt);
    }

    [Fact]
    public void MarkAsNoShow_WhenNotConfirmed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        registration.Cancel(); // Set to cancelled
        registration.ClearDomainEvents();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => registration.MarkAsNoShow());
        AssertTrue(exception.Message.Contains("Only confirmed registrations can be marked as no-show"));
    }

    [Fact]
    public void UpdateWaitlistPosition_WhenWaitlisted_ShouldUpdatePosition()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        AssertEqual(RegistrationStatus.Waitlisted, registration2.Status);

        // Act
        registration2.UpdateWaitlistPosition(5);

        // Assert
        AssertEqual(5, registration2.PositionInQueue);
        // Note: UpdateWaitlistPosition doesn't set UpdatedAt, only domain events
        AssertCollectionCount(registration2.DomainEvents, 2); // Created + PositionChanged
        AssertTrue(registration2.DomainEvents.Any(e => e is WaitlistPositionChangedDomainEvent));
    }

    [Fact]
    public void UpdateWaitlistPosition_WhenNotWaitlisted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        AssertEqual(RegistrationStatus.Confirmed, registration.Status);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => registration.UpdateWaitlistPosition(1));
        AssertTrue(exception.Message.Contains("Cannot set position for non-waitlisted registration"));
    }

    [Fact]
    public void IsWaitlisted_WhenWaitlistedWithPosition_ShouldReturnTrue()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        registration2.UpdateWaitlistPosition(1);

        // Act & Assert
        AssertTrue(registration2.IsWaitlisted());
    }

    [Fact]
    public void IsWaitlisted_WhenConfirmed_ShouldReturnFalse()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);

        // Act & Assert
        AssertFalse(registration.IsWaitlisted());
    }

    [Fact]
    public void IsWaitlisted_WhenWaitlistedWithoutPosition_ShouldReturnFalse()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        // Don't set position

        // Act & Assert
        AssertFalse(registration2.IsWaitlisted());
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllDomainEvents()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        AssertCollectionCount(registration.DomainEvents, 1);

        // Act
        registration.ClearDomainEvents();

        // Assert
        AssertCollectionEmpty(registration.DomainEvents);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidNotes_ShouldCreateRegistrationWithNotes(string? invalidNotes)
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();

        // Act
        var registration = new EventRegistration(@event, user, invalidNotes);

        // Assert
        AssertEqual(invalidNotes, registration.Notes); // Should accept any notes value
    }

    [Fact]
    public void Constructor_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        Event? @event = null;
        var user = Fixture.Create<User>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new EventRegistration(@event!, user));
        AssertTrue(exception.ParamName?.Contains("event", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void Constructor_WithNullUser_ShouldThrowArgumentNullException()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        User? user = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new EventRegistration(@event, user!));
        AssertTrue(exception.ParamName?.Contains("user", StringComparison.OrdinalIgnoreCase) == true);
    }

    // ===== NEW WAITLIST FUNCTIONALITY TESTS =====

    [Fact]
    public void AddToWaitlist_WhenNotWaitlisted_ShouldSetStatusToWaitlisted()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        registration.ClearDomainEvents(); // Clear initial event

        // Act
        registration.AddToWaitlist(5);

        // Assert
        AssertEqual(RegistrationStatus.Waitlisted, registration.Status);
        AssertEqual(5, registration.PositionInQueue);
        AssertCollectionCount(registration.DomainEvents, 1);
        AssertTrue(registration.DomainEvents.First() is RegistrationWaitlistedDomainEvent);
    }

    [Fact]
    public void AddToWaitlist_WhenAlreadyWaitlisted_ShouldDoNothing()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        AssertEqual(RegistrationStatus.Waitlisted, registration2.Status);
        registration2.ClearDomainEvents(); // Clear initial event

        // Act
        registration2.AddToWaitlist(10);

        // Assert
        AssertEqual(RegistrationStatus.Waitlisted, registration2.Status);
        AssertCollectionEmpty(registration2.DomainEvents); // Should not add new event
    }

    [Fact]
    public void AddToWaitlist_WithNullPosition_ShouldSetStatusToWaitlisted()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        registration.ClearDomainEvents(); // Clear initial event

        // Act
        registration.AddToWaitlist(null);

        // Assert
        AssertEqual(RegistrationStatus.Waitlisted, registration.Status);
        AssertNull(registration.PositionInQueue);
        AssertCollectionCount(registration.DomainEvents, 1);
        AssertTrue(registration.DomainEvents.First() is RegistrationWaitlistedDomainEvent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void UpdateWaitlistPosition_WithInvalidPosition_ShouldThrowArgumentException(int invalidPosition)
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        AssertEqual(RegistrationStatus.Waitlisted, registration2.Status);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => registration2.UpdateWaitlistPosition(invalidPosition));
        AssertTrue(exception.Message.Contains("Position must be positive"));
        AssertEqual("position", exception.ParamName);
    }

    [Fact]
    public void UpdateWaitlistPosition_WithSamePosition_ShouldNotRaiseDomainEvent()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        registration2.UpdateWaitlistPosition(5);
        registration2.ClearDomainEvents(); // Clear previous events

        // Act
        registration2.UpdateWaitlistPosition(5); // Same position

        // Assert
        AssertEqual(5, registration2.PositionInQueue);
        AssertCollectionEmpty(registration2.DomainEvents); // Should not raise event for same position
    }

    [Fact]
    public void UpdateWaitlistPosition_WithDifferentPosition_ShouldRaiseDomainEvent()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        registration2.UpdateWaitlistPosition(5);
        registration2.ClearDomainEvents(); // Clear previous events

        // Act
        registration2.UpdateWaitlistPosition(3); // Different position

        // Assert
        AssertEqual(3, registration2.PositionInQueue);
        AssertCollectionCount(registration2.DomainEvents, 1);
        AssertTrue(registration2.DomainEvents.First() is WaitlistPositionChangedDomainEvent);
        
        var domainEvent = registration2.DomainEvents.First() as WaitlistPositionChangedDomainEvent;
        AssertNotNull(domainEvent);
        AssertEqual(@event.Id, domainEvent!.EventId);
        AssertEqual(registration2.Id, domainEvent.RegistrationId);
        AssertEqual(5, domainEvent.OldPosition);
        AssertEqual(3, domainEvent.NewPosition);
    }

    [Fact]
    public void RemoveFromWaitlist_WhenWaitlisted_ShouldSetStatusToCancelled()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        registration2.UpdateWaitlistPosition(1);
        registration2.ClearDomainEvents(); // Clear previous events

        // Act
        registration2.RemoveFromWaitlist("User requested removal");

        // Assert
        AssertEqual(RegistrationStatus.Cancelled, registration2.Status);
        AssertNull(registration2.PositionInQueue);
        AssertCollectionCount(registration2.DomainEvents, 1);
        AssertTrue(registration2.DomainEvents.First() is WaitlistRemovalDomainEvent);
        
        var domainEvent = registration2.DomainEvents.First() as WaitlistRemovalDomainEvent;
        AssertNotNull(domainEvent);
        AssertEqual(registration2, domainEvent!.Registration);
        AssertEqual("User requested removal", domainEvent.Reason);
    }

    [Fact]
    public void RemoveFromWaitlist_WhenNotWaitlisted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        AssertEqual(RegistrationStatus.Confirmed, registration.Status);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => registration.RemoveFromWaitlist());
        AssertTrue(exception.Message.Contains("Registration is not on the waitlist"));
    }

    [Fact]
    public void RemoveFromWaitlist_WithNullReason_ShouldSetReasonToNull()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        registration2.UpdateWaitlistPosition(1);
        registration2.ClearDomainEvents(); // Clear previous events

        // Act
        registration2.RemoveFromWaitlist((string?)null);

        // Assert
        AssertEqual(RegistrationStatus.Cancelled, registration2.Status);
        AssertCollectionCount(registration2.DomainEvents, 1);
        AssertTrue(registration2.DomainEvents.First() is WaitlistRemovalDomainEvent);
        
        var domainEvent = registration2.DomainEvents.First() as WaitlistRemovalDomainEvent;
        AssertNotNull(domainEvent);
        AssertNull(domainEvent!.Reason);
    }

    [Fact]
    public void IsWaitlisted_WhenWaitlistedWithZeroPosition_ShouldReturnFalse()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        // Manually set invalid position (this would normally be prevented by UpdateWaitlistPosition)
        typeof(EventRegistration).GetProperty("PositionInQueue")!.SetValue(registration2, 0);

        // Act & Assert
        AssertFalse(registration2.IsWaitlisted());
    }

    [Fact]
    public void IsWaitlisted_WhenWaitlistedWithNegativePosition_ShouldReturnFalse()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        // Manually set invalid position (this would normally be prevented by UpdateWaitlistPosition)
        typeof(EventRegistration).GetProperty("PositionInQueue")!.SetValue(registration2, -1);

        // Act & Assert
        AssertFalse(registration2.IsWaitlisted());
    }

    [Fact]
    public void IsWaitlisted_WhenCancelled_ShouldReturnFalse()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 1, "UTC", "Test Location", "organizer-123");
        var user1 = Fixture.Create<User>();
        var user2 = Fixture.Create<User>();
        
        var registration1 = new EventRegistration(@event, user1);
        @event.AddRegistration(registration1);
        
        var registration2 = new EventRegistration(@event, user2);
        registration2.UpdateWaitlistPosition(1);
        registration2.Cancel();

        // Act & Assert
        AssertFalse(registration2.IsWaitlisted());
    }
}