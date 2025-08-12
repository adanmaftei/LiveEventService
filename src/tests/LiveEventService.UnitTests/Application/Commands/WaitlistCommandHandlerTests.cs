using AutoMapper;
using LiveEventService.Application.Features.Events.EventRegistration.Register;
using LiveEventService.Application.Features.Events.Commands.ConfirmRegistration;
using LiveEventService.Application.Features.Events.EventRegistration;
using LiveEventService.Core.Events;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Users.User;
using LiveEventService.Core.Common;
using LiveEventService.UnitTests.Common;

namespace LiveEventService.UnitTests.Application.Commands;

public class WaitlistCommandHandlerTests : TestBase
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRepository<EventRegistration>> _mockRegistrationRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RegisterForEventCommandHandler _registerHandler;
    private readonly ConfirmRegistrationCommandHandler _confirmHandler;
    private readonly CancelEventRegistrationCommandHandler _cancelHandler;

    public WaitlistCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRegistrationRepository = new Mock<IRepository<EventRegistration>>();
        _mockMapper = new Mock<IMapper>();
        
        _registerHandler = new RegisterForEventCommandHandler(
            _mockEventRepository.Object, 
            _mockUserRepository.Object, 
            _mockRegistrationRepository.Object, 
            _mockMapper.Object);
            
        _confirmHandler = new ConfirmRegistrationCommandHandler(
            _mockRegistrationRepository.Object, 
            _mockMapper.Object);
            
        _cancelHandler = new CancelEventRegistrationCommandHandler(
            _mockRegistrationRepository.Object, 
            _mockUserRepository.Object);
    }

    [Fact]
    public async Task RegisterForEvent_WhenEventHasCapacity_ShouldCreateConfirmedRegistration()
    {
        // Arrange
        var command = new RegisterForEventCommand
        {
            EventId = Guid.NewGuid(),
            UserId = "user-123",
            Notes = "Test registration"
        };

        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        @event.Publish(); // Make event published
        
        var user = new User("user-123", "test@example.com", "John", "Doe", "1234567890");
        var registration = new EventRegistration(@event, user, command.Notes);
        var registrationDto = Fixture.Create<EventRegistrationDto>();

        _mockEventRepository.Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);
        _mockUserRepository.Setup(x => x.GetByIdentityIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockEventRepository.Setup(x => x.IsUserRegisteredForEventAsync(command.EventId, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockEventRepository.Setup(x => x.GetRegistrationCountForEventAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50); // Event has capacity
        _mockRegistrationRepository.Setup(x => x.AddAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _mockMapper.Setup(x => x.Map<EventRegistrationDto>(It.IsAny<EventRegistration>()))
            .Returns(registrationDto);

        // Act
        var result = await _registerHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertTrue(result.Success);
        AssertNotNull(result.Data);
        AssertEqual(registrationDto, result.Data);
        AssertTrue(result.Message!.Contains("successfully registered"));

        _mockEventRepository.Verify(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(x => x.GetByIdentityIdAsync(command.UserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRegistrationRepository.Verify(x => x.AddAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterForEvent_WhenEventIsFull_ShouldCreateWaitlistedRegistration()
    {
        // Arrange
        var command = new RegisterForEventCommand
        {
            EventId = Guid.NewGuid(),
            UserId = "user-123",
            Notes = "Test registration"
        };

        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        @event.Publish(); // Make event published
        
        var user = new User("user-123", "test@example.com", "John", "Doe", "1234567890");
        var registrationDto = Fixture.Create<EventRegistrationDto>();

        _mockEventRepository.Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);
        _mockUserRepository.Setup(x => x.GetByIdentityIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockEventRepository.Setup(x => x.IsUserRegisteredForEventAsync(command.EventId, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockEventRepository.Setup(x => x.GetRegistrationCountForEventAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100); // Event is full
        _mockEventRepository.Setup(x => x.CalculateWaitlistPositionAsync(command.EventId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockRegistrationRepository.Setup(x => x.AddAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()))
            .Returns<EventRegistration, CancellationToken>((reg, token) => Task.FromResult(reg));
        _mockRegistrationRepository.Setup(x => x.UpdateAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockMapper.Setup(x => x.Map<EventRegistrationDto>(It.IsAny<EventRegistration>()))
            .Returns(registrationDto);

        // Act
        var result = await _registerHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertTrue(result.Success);
        AssertNotNull(result.Data);
        AssertEqual(registrationDto, result.Data);
        AssertTrue(result.Message!.Contains("You have been added to the waitlist"));

        _mockEventRepository.Verify(x => x.CalculateWaitlistPositionAsync(command.EventId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRegistrationRepository.Verify(x => x.UpdateAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterForEvent_WhenEventNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterForEventCommand
        {
            EventId = Guid.NewGuid(),
            UserId = "user-123"
        };

        _mockEventRepository.Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _registerHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertFalse(result.Success);
        AssertNull(result.Data);
        AssertTrue(result.Message!.Contains("Event not found"));
    }

    [Fact]
    public async Task RegisterForEvent_WhenEventNotPublished_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterForEventCommand
        {
            EventId = Guid.NewGuid(),
            UserId = "user-123"
        };

        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        // Event is not published

        _mockEventRepository.Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _registerHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertFalse(result.Success);
        AssertNull(result.Data);
        AssertTrue(result.Message!.Contains("not currently accepting registrations"));
    }

    [Fact]
    public async Task RegisterForEvent_WhenEventAlreadyStarted_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterForEventCommand
        {
            EventId = Guid.NewGuid(),
            UserId = "user-123"
        };

        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1), 100, "UTC", "Test Location", "organizer-123");
        @event.Publish();

        _mockEventRepository.Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _registerHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertFalse(result.Success);
        AssertNull(result.Data);
        AssertTrue(result.Message!.Contains("already started"));
    }

    [Fact]
    public async Task RegisterForEvent_WhenUserAlreadyRegistered_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterForEventCommand
        {
            EventId = Guid.NewGuid(),
            UserId = "user-123"
        };

        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        @event.Publish();
        
        var user = new User("user-123", "test@example.com", "John", "Doe", "1234567890");

        _mockEventRepository.Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);
        _mockUserRepository.Setup(x => x.GetByIdentityIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockEventRepository.Setup(x => x.IsUserRegisteredForEventAsync(command.EventId, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // User already registered

        // Act
        var result = await _registerHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertFalse(result.Success);
        AssertNull(result.Data);
        AssertTrue(result.Message!.Contains("already registered"));
    }

    [Fact]
    public async Task ConfirmRegistration_WhenWaitlisted_ShouldConfirmRegistration()
    {
        // Arrange
        var command = new ConfirmRegistrationCommand
        {
            RegistrationId = Guid.NewGuid(),
            AdminUserId = "admin-123"
        };

        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = new User("user-123", "test@example.com", "John", "Doe", "1234567890");
        var registration = new EventRegistration(@event, user);
        registration.AddToWaitlist(1); // Make it waitlisted
        
        var registrationDto = Fixture.Create<EventRegistrationDto>();

        _mockRegistrationRepository.Setup(x => x.GetByIdAsync(command.RegistrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _mockRegistrationRepository.Setup(x => x.UpdateAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockMapper.Setup(x => x.Map<EventRegistrationDto>(It.IsAny<EventRegistration>()))
            .Returns(registrationDto);

        // Act
        var result = await _confirmHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertTrue(result.Success);
        AssertNotNull(result.Data);
        AssertEqual(registrationDto, result.Data);
        AssertTrue(result.Message!.Contains("confirmed successfully"));

        _mockRegistrationRepository.Verify(x => x.UpdateAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmRegistration_WhenRegistrationNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new ConfirmRegistrationCommand
        {
            RegistrationId = Guid.NewGuid(),
            AdminUserId = "admin-123"
        };

        _mockRegistrationRepository.Setup(x => x.GetByIdAsync(command.RegistrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventRegistration?)null);

        // Act
        var result = await _confirmHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertFalse(result.Success);
        AssertNull(result.Data);
        AssertTrue(result.Message!.Contains("Registration not found"));
    }

    [Fact]
    public async Task ConfirmRegistration_WhenAlreadyConfirmed_ShouldReturnFailure()
    {
        // Arrange
        var command = new ConfirmRegistrationCommand
        {
            RegistrationId = Guid.NewGuid(),
            AdminUserId = "admin-123"
        };

        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = new User("user-123", "test@example.com", "John", "Doe", "1234567890");
        var registration = new EventRegistration(@event, user);
        registration.Confirm(); // Already confirmed

        _mockRegistrationRepository.Setup(x => x.GetByIdAsync(command.RegistrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);

        // Act
        var result = await _confirmHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertFalse(result.Success);
        AssertNull(result.Data);
        AssertTrue(result.Message!.Contains("already confirmed"));
    }

    [Fact]
    public async Task CancelEventRegistration_WhenConfirmedRegistration_ShouldCancelAndPromoteWaitlisted()
    {
        // Arrange
        var command = new CancelEventRegistrationCommand
        {
            RegistrationId = Guid.NewGuid(),
            UserId = "admin-123",
            IsAdmin = true
        };

        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = new User("user-123", "test@example.com", "John", "Doe", "1234567890");
        var registration = new EventRegistration(@event, user);
        registration.Confirm(); // Make it confirmed
        
        var waitlistedUser = new User("user-456", "waitlist@example.com", "Jane", "Smith", "0987654321");


        _mockRegistrationRepository.Setup(x => x.GetByIdAsync(command.RegistrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _mockRegistrationRepository.Setup(x => x.UpdateAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);


        // Act
        var result = await _cancelHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertTrue(result.Success);
        AssertTrue(result.Data);
        AssertTrue(result.Message!.Contains("cancelled"));

        // Should update the cancelled registration
        _mockRegistrationRepository.Verify(x => x.UpdateAsync(It.Is<EventRegistration>(r => r.Id == registration.Id), It.IsAny<CancellationToken>()), Times.Once);
        // Note: Waitlist promotion is now handled by domain event handlers, not directly in the command handler
    }

    [Fact]
    public async Task CancelEventRegistration_WhenRegistrationNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new CancelEventRegistrationCommand
        {
            RegistrationId = Guid.NewGuid(),
            UserId = "user-123",
            IsAdmin = false
        };

        _mockRegistrationRepository.Setup(x => x.GetByIdAsync(command.RegistrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventRegistration?)null);

        // Act
        var result = await _cancelHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertFalse(result.Success);
        AssertFalse(result.Data);
        AssertTrue(result.Message!.Contains("Registration not found"));
    }

    [Fact]
    public async Task CancelEventRegistration_WhenNotAuthorized_ShouldReturnFailure()
    {
        // Arrange
        var command = new CancelEventRegistrationCommand
        {
            RegistrationId = Guid.NewGuid(),
            UserId = "user-123",
            IsAdmin = false
        };

        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = new User("user-123", "test@example.com", "John", "Doe", "1234567890");
        var registration = new EventRegistration(@event, user);
        registration.Confirm();

        var differentUser = new User("user-456", "different@example.com", "Jane", "Smith", "0987654321");

        _mockRegistrationRepository.Setup(x => x.GetByIdAsync(command.RegistrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _mockUserRepository.Setup(x => x.GetByIdentityIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(differentUser); // Different user

        // Act
        var result = await _cancelHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertFalse(result.Success);
        AssertFalse(result.Data);
        AssertTrue(result.Message!.Contains("Not authorized"));
    }

    [Fact]
    public async Task CancelEventRegistration_WhenAdmin_ShouldAllowCancellation()
    {
        // Arrange
        var command = new CancelEventRegistrationCommand
        {
            RegistrationId = Guid.NewGuid(),
            UserId = "admin-123",
            IsAdmin = true
        };

        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var user = new User("user-123", "test@example.com", "John", "Doe", "1234567890");
        var registration = new EventRegistration(@event, user);
        registration.Confirm();

        _mockRegistrationRepository.Setup(x => x.GetByIdAsync(command.RegistrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _mockRegistrationRepository.Setup(x => x.UpdateAsync(It.IsAny<EventRegistration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRegistrationRepository.Setup(x => x.ListAsync(It.IsAny<ISpecification<EventRegistration>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventRegistration>()); // No waitlisted registrations

        // Act
        var result = await _cancelHandler.Handle(command, CancellationToken.None);

        // Assert
        AssertTrue(result.Success);
        AssertTrue(result.Data);
        AssertTrue(result.Message!.Contains("cancelled"));

        // Should not check user authorization when admin
        _mockUserRepository.Verify(x => x.GetByIdentityIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
} 