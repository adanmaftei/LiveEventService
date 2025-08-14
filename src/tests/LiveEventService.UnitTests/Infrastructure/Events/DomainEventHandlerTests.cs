using LiveEventService.Application.Common.Notifications;
using LiveEventService.Application.Features.Events.DomainEventHandlers;
using LiveEventService.Core.Common;
using LiveEventService.Core.Events;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Users.User;
using LiveEventService.UnitTests.Common;
using Microsoft.Extensions.Logging;

namespace LiveEventService.UnitTests.Infrastructure.Events;

public class DomainEventHandlerTests : TestBase
{
    private readonly Mock<IEventRegistrationNotifier> _mockNotifier;
    private readonly Mock<ILogger<WaitlistPositionChangedDomainEventHandler>> _mockPositionLogger;
    private readonly Mock<ILogger<WaitlistRemovalDomainEventHandler>> _mockRemovalLogger;
    private readonly Mock<ILogger<EventRegistrationCancelledDomainEventHandler>> _mockCancelledLogger;
    private readonly Mock<IRepository<Event>> _mockEventRepository;
    private readonly Mock<IRepository<EventRegistration>> _mockRegistrationRepository;

    public DomainEventHandlerTests()
    {
        _mockNotifier = new Mock<IEventRegistrationNotifier>();
        _mockPositionLogger = new Mock<ILogger<WaitlistPositionChangedDomainEventHandler>>();
        _mockRemovalLogger = new Mock<ILogger<WaitlistRemovalDomainEventHandler>>();
        _mockCancelledLogger = new Mock<ILogger<EventRegistrationCancelledDomainEventHandler>>();
        _mockEventRepository = new Mock<IRepository<Event>>();
        _mockRegistrationRepository = new Mock<IRepository<EventRegistration>>();
    }

    // ===== EVENT REGISTRATION DOMAIN EVENT HANDLERS =====

    [Fact]
    public async Task EventRegistrationCreatedDomainEventHandler_ShouldCallNotifierWithCorrectParameters()
    {
        // Arrange
        var handler = new EventRegistrationCreatedDomainEventHandler(_mockNotifier.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new EventRegistrationCreatedDomainEvent(registration);
        var notification = new EventRegistrationCreatedNotification(domainEvent);

        _mockNotifier.Setup(x => x.NotifyAsync(It.IsAny<EventRegistration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockNotifier.Verify(x => x.NotifyAsync(registration, "created", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EventRegistrationPromotedDomainEventHandler_ShouldCallNotifierWithCorrectParameters()
    {
        // Arrange
        var handler = new EventRegistrationPromotedDomainEventHandler(_mockNotifier.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new EventRegistrationPromotedDomainEvent(registration);
        var notification = new EventRegistrationPromotedNotification(domainEvent);

        _mockNotifier.Setup(x => x.NotifyAsync(It.IsAny<EventRegistration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockNotifier.Verify(x => x.NotifyAsync(registration, "promoted", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EventRegistrationCancelledDomainEventHandler_ShouldCallNotifierWithCorrectParameters()
    {
        // Arrange
        var handler = new EventRegistrationCancelledDomainEventHandler(_mockNotifier.Object, _mockRegistrationRepository.Object, _mockEventRepository.Object, _mockCancelledLogger.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new EventRegistrationCancelledDomainEvent(registration);
        var notification = new EventRegistrationCancelledNotification(domainEvent);

        _mockNotifier.Setup(x => x.NotifyAsync(It.IsAny<EventRegistration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockNotifier.Verify(x => x.NotifyAsync(registration, "cancelled", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EventRegistrationCreatedDomainEventHandler_WhenNotifierThrowsException_ShouldPropagateException()
    {
        // Arrange
        var handler = new EventRegistrationCreatedDomainEventHandler(_mockNotifier.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new EventRegistrationCreatedDomainEvent(registration);
        var notification = new EventRegistrationCreatedNotification(domainEvent);

        var expectedException = new InvalidOperationException("Notifier failed");
        _mockNotifier.Setup(x => x.NotifyAsync(It.IsAny<EventRegistration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(notification, CancellationToken.None));
        AssertEqual(expectedException, exception);
    }

    // ===== WAITLIST POSITION CHANGED DOMAIN EVENT HANDLER =====

    [Fact]
    public async Task WaitlistPositionChangedDomainEventHandler_ShouldLogPositionChange()
    {
        // Arrange
        var handler = new WaitlistPositionChangedDomainEventHandler(_mockPositionLogger.Object, _mockEventRepository.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistPositionChangedDomainEvent(@event.Id, registration.Id, 5, 3);
        var notification = new WaitlistPositionChangedNotification(domainEvent);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockPositionLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Waitlist position updated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WaitlistPositionChangedDomainEventHandler_WhenMovingToTop5_ShouldLogSpecialMessage()
    {
        // Arrange
        var handler = new WaitlistPositionChangedDomainEventHandler(_mockPositionLogger.Object, _mockEventRepository.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistPositionChangedDomainEvent(@event.Id, registration.Id, 10, 3); // Moving from 10 to 3
        var notification = new WaitlistPositionChangedNotification(domainEvent);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockPositionLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("top 5 waitlist positions")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WaitlistPositionChangedDomainEventHandler_WhenMovingFromTop5_ShouldNotLogSpecialMessage()
    {
        // Arrange
        var handler = new WaitlistPositionChangedDomainEventHandler(_mockPositionLogger.Object, _mockEventRepository.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistPositionChangedDomainEvent(@event.Id, registration.Id, 3, 10); // Moving from 3 to 10
        var notification = new WaitlistPositionChangedNotification(domainEvent);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockPositionLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("top 5 waitlist positions")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task WaitlistPositionChangedDomainEventHandler_WhenPositionIsNull_ShouldHandleNullValues()
    {
        // Arrange
        var handler = new WaitlistPositionChangedDomainEventHandler(_mockPositionLogger.Object, _mockEventRepository.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistPositionChangedDomainEvent(@event.Id, registration.Id, null, 1); // From null to 1
        var notification = new WaitlistPositionChangedNotification(domainEvent);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockPositionLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Waitlist position updated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ===== WAITLIST REMOVAL DOMAIN EVENT HANDLER =====

    [Fact]
    public async Task WaitlistRemovalDomainEventHandler_WhenEventFound_ShouldUpdateWaitlistPositions()
    {
        // Arrange
        var handler = new WaitlistRemovalDomainEventHandler(_mockRemovalLogger.Object, _mockEventRepository.Object, _mockRegistrationRepository.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistRemovalDomainEvent(registration, "User requested removal");
        var notification = new WaitlistRemovalNotification(domainEvent);

        _mockRegistrationRepository.Setup(x => x.ListAsync(It.IsAny<WaitlistedRegistrationsForEventSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventRegistration>());
        _mockRegistrationRepository.Setup(x => x.UpdateAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockRegistrationRepository.Verify(x => x.ListAsync(It.IsAny<WaitlistedRegistrationsForEventSpecification>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRemovalLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Reason: User requested removal")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WaitlistRemovalDomainEventHandler_WhenEventNotFound_ShouldLogErrorAndReturn()
    {
        // Arrange
        var handler = new WaitlistRemovalDomainEventHandler(_mockRemovalLogger.Object, _mockEventRepository.Object, _mockRegistrationRepository.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistRemovalDomainEvent(registration, "User requested removal");
        var notification = new WaitlistRemovalNotification(domainEvent);

        _mockRegistrationRepository.Setup(x => x.ListAsync(It.IsAny<WaitlistedRegistrationsForEventSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventRegistration>());

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockRegistrationRepository.Verify(x => x.ListAsync(It.IsAny<WaitlistedRegistrationsForEventSpecification>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRemovalLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Reason: User requested removal")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WaitlistRemovalDomainEventHandler_WhenReasonIsNull_ShouldLogWithoutReason()
    {
        // Arrange
        var handler = new WaitlistRemovalDomainEventHandler(_mockRemovalLogger.Object, _mockEventRepository.Object, _mockRegistrationRepository.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistRemovalDomainEvent(registration, null); // No reason
        var notification = new WaitlistRemovalNotification(domainEvent);

        _mockRegistrationRepository.Setup(x => x.ListAsync(It.IsAny<WaitlistedRegistrationsForEventSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventRegistration>());
        _mockRegistrationRepository.Setup(x => x.UpdateAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockRemovalLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("removed from waitlist") && !v.ToString()!.Contains("Reason:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WaitlistRemovalDomainEventHandler_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var handler = new WaitlistRemovalDomainEventHandler(_mockRemovalLogger.Object, _mockEventRepository.Object, _mockRegistrationRepository.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistRemovalDomainEvent(registration, "User requested removal");
        var notification = new WaitlistRemovalNotification(domainEvent);

        var expectedException = new InvalidOperationException("Repository failed");
        _mockRegistrationRepository.Setup(x => x.ListAsync(It.IsAny<WaitlistedRegistrationsForEventSpecification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(notification, CancellationToken.None));
        AssertEqual(expectedException, exception);
    }

    [Fact]
    public async Task WaitlistRemovalDomainEventHandler_ShouldLogFinalUpdateMessage()
    {
        // Arrange
        var handler = new WaitlistRemovalDomainEventHandler(_mockRemovalLogger.Object, _mockEventRepository.Object, _mockRegistrationRepository.Object);
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = Fixture.Create<User>();
        var registration = new EventRegistration(@event, user);
        var domainEvent = new WaitlistRemovalDomainEvent(registration, "User requested removal");
        var notification = new WaitlistRemovalNotification(domainEvent);


        _mockRegistrationRepository.Setup(x => x.ListAsync(It.IsAny<WaitlistedRegistrationsForEventSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventRegistration>());
        _mockRegistrationRepository.Setup(x => x.UpdateAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockRemovalLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated waitlist positions after removal")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
