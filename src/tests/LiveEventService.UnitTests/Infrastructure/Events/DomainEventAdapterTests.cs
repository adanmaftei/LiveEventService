using LiveEventService.Core.Events;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Users.User;
using LiveEventService.Application.Common.Notifications;
using LiveEventService.UnitTests.Common;
using MediatR;

namespace LiveEventService.UnitTests.Infrastructure.Events;

public class DomainEventAdapterTests : TestBase
{
    // ===== EVENT REGISTRATION DOMAIN EVENT ADAPTERS =====

    [Fact]
    public void EventRegistrationCreatedNotification_ShouldWrapDomainEventCorrectly()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new EventRegistrationCreatedDomainEvent(registration);

        // Act
        var notification = new EventRegistrationCreatedNotification(domainEvent);

        // Assert
        AssertNotNull(notification);
        AssertEqual(domainEvent, notification.DomainEvent);
        AssertEqual(registration, notification.DomainEvent.Registration);
    }

    [Fact]
    public void EventRegistrationPromotedNotification_ShouldWrapDomainEventCorrectly()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new EventRegistrationPromotedDomainEvent(registration);

        // Act
        var notification = new EventRegistrationPromotedNotification(domainEvent);

        // Assert
        AssertNotNull(notification);
        AssertEqual(domainEvent, notification.DomainEvent);
        AssertEqual(registration, notification.DomainEvent.Registration);
    }

    [Fact]
    public void EventRegistrationCancelledNotification_ShouldWrapDomainEventCorrectly()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new EventRegistrationCancelledDomainEvent(registration);

        // Act
        var notification = new EventRegistrationCancelledNotification(domainEvent);

        // Assert
        AssertNotNull(notification);
        AssertEqual(domainEvent, notification.DomainEvent);
        AssertEqual(registration, notification.DomainEvent.Registration);
    }

    // ===== WAITLIST DOMAIN EVENT ADAPTERS =====

    [Fact]
    public void EventCapacityIncreasedNotification_ShouldWrapDomainEventCorrectly()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var domainEvent = new EventCapacityIncreasedDomainEvent(@event, 50);

        // Act
        var notification = new EventCapacityIncreasedNotification(domainEvent);

        // Assert
        AssertNotNull(notification);
        AssertEqual(domainEvent, notification.DomainEvent);
        AssertEqual(@event, notification.DomainEvent.Event);
        AssertEqual(50, notification.DomainEvent.AdditionalCapacity);
    }

    [Fact]
    public void EventCapacityIncreasedNotification_WithNullDomainEvent_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new EventCapacityIncreasedNotification(null!));
        AssertEqual("domainEvent", exception.ParamName);
    }

    [Fact]
    public void RegistrationWaitlistedNotification_ShouldWrapDomainEventCorrectly()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new RegistrationWaitlistedDomainEvent(registration);

        // Act
        var notification = new RegistrationWaitlistedNotification(domainEvent);

        // Assert
        AssertNotNull(notification);
        AssertEqual(domainEvent, notification.DomainEvent);
        AssertEqual(registration, notification.DomainEvent.Registration);
    }

    [Fact]
    public void RegistrationWaitlistedNotification_WithNullDomainEvent_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new RegistrationWaitlistedNotification(null!));
        AssertEqual("domainEvent", exception.ParamName);
    }

    [Fact]
    public void WaitlistPositionChangedNotification_ShouldWrapDomainEventCorrectly()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistPositionChangedDomainEvent(@event.Id, registration.Id, 5, 3);

        // Act
        var notification = new WaitlistPositionChangedNotification(domainEvent);

        // Assert
        AssertNotNull(notification);
        AssertEqual(domainEvent, notification.DomainEvent);
        AssertEqual(@event.Id, notification.DomainEvent.EventId);
        AssertEqual(registration.Id, notification.DomainEvent.RegistrationId);
        AssertEqual(5, notification.DomainEvent.OldPosition);
        AssertEqual(3, notification.DomainEvent.NewPosition);
    }

    [Fact]
    public void WaitlistPositionChangedNotification_WithNullDomainEvent_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WaitlistPositionChangedNotification(null!));
        AssertEqual("domainEvent", exception.ParamName);
    }

    [Fact]
    public void WaitlistRemovalNotification_ShouldWrapDomainEventCorrectly()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistRemovalDomainEvent(registration, "User requested removal");

        // Act
        var notification = new WaitlistRemovalNotification(domainEvent);

        // Assert
        AssertNotNull(notification);
        AssertEqual(domainEvent, notification.DomainEvent);
        AssertEqual(registration, notification.DomainEvent.Registration);
        AssertEqual("User requested removal", notification.DomainEvent.Reason);
    }

    [Fact]
    public void WaitlistRemovalNotification_WithNullDomainEvent_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WaitlistRemovalNotification(null!));
        AssertEqual("domainEvent", exception.ParamName);
    }

    [Fact]
    public void WaitlistRemovalNotification_WithNullReason_ShouldHandleNullReason()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistRemovalDomainEvent(registration, null);

        // Act
        var notification = new WaitlistRemovalNotification(domainEvent);

        // Assert
        AssertNotNull(notification);
        AssertEqual(domainEvent, notification.DomainEvent);
        AssertEqual(registration, notification.DomainEvent.Registration);
        AssertNull(notification.DomainEvent.Reason);
    }

    // ===== EDGE CASES AND VALIDATION =====

    [Fact]
    public void AllNotificationTypes_ShouldImplementINotification()
    {
        // Arrange & Act & Assert
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);

        // Event Registration notifications
        var createdEvent = new EventRegistrationCreatedDomainEvent(registration);
        var promotedEvent = new EventRegistrationPromotedDomainEvent(registration);
        var cancelledEvent = new EventRegistrationCancelledDomainEvent(registration);

        var createdNotification = new EventRegistrationCreatedNotification(createdEvent);
        var promotedNotification = new EventRegistrationPromotedNotification(promotedEvent);
        var cancelledNotification = new EventRegistrationCancelledNotification(cancelledEvent);

        // Waitlist notifications
        var capacityEvent = new EventCapacityIncreasedDomainEvent(@event, 50);
        var waitlistedEvent = new RegistrationWaitlistedDomainEvent(registration);
        var positionEvent = new WaitlistPositionChangedDomainEvent(@event.Id, registration.Id, 5, 3);
        var removalEvent = new WaitlistRemovalDomainEvent(registration, "Test reason");

        var capacityNotification = new EventCapacityIncreasedNotification(capacityEvent);
        var waitlistedNotification = new RegistrationWaitlistedNotification(waitlistedEvent);
        var positionNotification = new WaitlistPositionChangedNotification(positionEvent);
        var removalNotification = new WaitlistRemovalNotification(removalEvent);

        // Verify all implement INotification
        AssertTrue(createdNotification is INotification);
        AssertTrue(promotedNotification is INotification);
        AssertTrue(cancelledNotification is INotification);
        AssertTrue(capacityNotification is INotification);
        AssertTrue(waitlistedNotification is INotification);
        AssertTrue(positionNotification is INotification);
        AssertTrue(removalNotification is INotification);
    }

    [Fact]
    public void NotificationProperties_ShouldBeReadOnly()
    {
        // Arrange
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new EventRegistrationCreatedDomainEvent(registration);
        var notification = new EventRegistrationCreatedNotification(domainEvent);

        // Act & Assert
        // Verify that DomainEvent property is read-only (getter only)
        var propertyInfo = typeof(EventRegistrationCreatedNotification).GetProperty("DomainEvent");
        AssertNotNull(propertyInfo);
        AssertTrue(propertyInfo!.CanRead);
        AssertFalse(propertyInfo.CanWrite);
    }
}
