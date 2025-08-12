using AutoMapper;
using LiveEventService.Application.Features.Events.Event.Create;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Core.Events;
using LiveEventService.Core.Users.User;
using LiveEventService.UnitTests.Common;

namespace LiveEventService.UnitTests.Application.Commands;

public class CreateEventCommandHandlerTests : TestBase
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly CreateEventCommandHandler _handler;

    public CreateEventCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _handler = new CreateEventCommandHandler(_mockEventRepository.Object, _mockUserRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateEvent()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Event = new CreateEventDto
            {
                Title = "Test Event",
                Description = "Test Description",
                StartDateTime = DateTime.UtcNow.AddDays(1),
                EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                Location = "Test Location",
                Capacity = 100,
                TimeZone = "UTC"
            },
            OrganizerId = "organizer-123"
        };

        var user = new User("auth0|testuser123", "test@example.com", "John", "Doe", "1234567890");
        var eventEntity = Fixture.Create<Event>();
        var eventDto = Fixture.Create<EventDto>();

        _mockUserRepository.Setup(x => x.GetByIdentityIdAsync("organizer-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockEventRepository.Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventEntity);

        _mockMapper.Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns(eventDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        AssertTrue(result.Success);
        AssertNotNull(result.Data);
        AssertEqual(eventDto, result.Data);
        AssertNull(result.Errors);

        _mockUserRepository.Verify(x => x.GetByIdentityIdAsync(command.OrganizerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockEventRepository.Verify(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(x => x.Map<EventDto>(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrganizerNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Event = new CreateEventDto
            {
                Title = "Test Event",
                Description = "Test Description",
                StartDateTime = DateTime.UtcNow.AddDays(1),
                EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                Location = "Test Location",
                Capacity = 100,
                TimeZone = "UTC"
            },
            OrganizerId = "organizer-123"
        };

        _mockUserRepository.Setup(x => x.GetByIdentityIdAsync("organizer-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        AssertFalse(result.Success);
        AssertNull(result.Data);
        AssertNotNull(result.Message);
        AssertTrue(result.Message!.Contains("Organizer not found"));

        _mockUserRepository.Verify(x => x.GetByIdentityIdAsync(command.OrganizerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventCreationFails_ShouldThrowException()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Event = new CreateEventDto
            {
                Title = "Test Event",
                Description = "Test Description",
                StartDateTime = DateTime.UtcNow.AddDays(1),
                EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                Location = "Test Location",
                Capacity = 100,
                TimeZone = "UTC"
            },
            OrganizerId = "organizer-123"
        };

        var user = new User("auth0|testuser123", "test@example.com", "John", "Doe", "1234567890");

        _mockUserRepository.Setup(x => x.GetByIdentityIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockEventRepository.Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
        AssertTrue(exception.Message.Contains("Database error"));

        _mockUserRepository.Verify(x => x.GetByIdentityIdAsync(command.OrganizerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockEventRepository.Verify(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMapperFails_ShouldThrowException()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Event = new CreateEventDto
            {
                Title = "Test Event",
                Description = "Test Description",
                StartDateTime = DateTime.UtcNow.AddDays(1),
                EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                Location = "Test Location",
                Capacity = 100,
                TimeZone = "UTC"
            },
            OrganizerId = "organizer-123"
        };

        var user = new User("auth0|testuser123", "test@example.com", "John", "Doe", "1234567890");

        _mockUserRepository.Setup(x => x.GetByIdentityIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockMapper.Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Throws(new Exception("Mapping error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
        AssertTrue(exception.Message.Contains("Mapping error"));

        _mockUserRepository.Verify(x => x.GetByIdentityIdAsync(command.OrganizerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockEventRepository.Verify(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(x => x.Map<EventDto>(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyOrganizerId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Event = new CreateEventDto
            {
                Title = "Test Event",
                Description = "Test Description",
                StartDateTime = DateTime.UtcNow.AddDays(1),
                EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                Location = "Test Location",
                Capacity = 100,
                TimeZone = "UTC"
            },
            OrganizerId = ""
        };

        _mockUserRepository.Setup(x => x.GetByIdentityIdAsync("", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        AssertFalse(result.Success);
        AssertNull(result.Data);
        AssertNotNull(result.Message);
        AssertTrue(result.Message!.Contains("Organizer not found"));

        _mockUserRepository.Verify(x => x.GetByIdentityIdAsync(command.OrganizerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassCancellationTokenToRepositories()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Event = new CreateEventDto
            {
                Title = "Test Event",
                Description = "Test Description",
                StartDateTime = DateTime.UtcNow.AddDays(1),
                EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                Location = "Test Location",
                Capacity = 100,
                TimeZone = "UTC"
            },
            OrganizerId = "organizer-123"
        };

        var user = new User("auth0|testuser123", "test@example.com", "John", "Doe", "1234567890");
        var eventEntity = Fixture.Create<Event>();
        var eventDto = Fixture.Create<EventDto>();
        var cancellationToken = new CancellationToken();

        _mockUserRepository.Setup(x => x.GetByIdentityIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockEventRepository.Setup(x => x.AddAsync(It.IsAny<Event>(), cancellationToken))
            .ReturnsAsync(eventEntity);

        _mockMapper.Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns(eventDto);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        AssertTrue(result.Success);
        _mockUserRepository.Verify(x => x.GetByIdentityIdAsync(command.OrganizerId, cancellationToken), Times.Once);
        _mockEventRepository.Verify(x => x.AddAsync(It.IsAny<Event>(), cancellationToken), Times.Once);
    }
}