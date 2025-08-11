using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LiveEventService.IntegrationTests.Infrastructure;
using LiveEventService.Infrastructure.Data;

namespace LiveEventService.IntegrationTests.Api;

public class EventEndpointsTests : BaseLiveEventsTests
{
    public EventEndpointsTests(LiveEventTestApplicationFactory factory)
        : base(factory)
    {        
    }

    [Fact]
    public async Task GetEvents_ShouldReturnPublishedEvents_WhenUnauthenticated()
    {        
        // Arrange - Create some test events
        await SeedEventsWithDifferentDates();
        
        // Act
        var response = await _unauthenticatedClient.GetAsync("/api/events?pageNumber=1&pageSize=10");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Status: {response.StatusCode}");
        Console.WriteLine($"Response Content: {content}");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnCreated_WhenAuthenticatedAsAdmin()
    {
        // Arrange        
        var eventData = TestDataBuilder.Commands.CreateEventCommand(
            name: "Test Event",
            capacity: 50
        );

        // Act
        var response = await _authenticatedAdminClient.PostAsJsonAsync("/api/events", eventData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();

        // Verify in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();

        var eventInDb = await dbContext.Events.FirstOrDefaultAsync(e => e.Name == "Test Event");
        eventInDb.Should().NotBeNull();
        eventInDb!.Capacity.Should().Be(50);
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnUnauthorized_WhenUnauthenticated()
    {
        // Arrange
        var eventData = TestDataBuilder.Commands.CreateEventCommand();

        // Act
        var response = await _unauthenticatedClient.PostAsJsonAsync("/api/events", eventData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnForbidden_WhenAuthenticatedAsParticipant()
    {
        // Arrange
        var participantClient = _factory.CreateAuthenticatedClient("participant-user", "Participant");
        var eventData = TestDataBuilder.Commands.CreateEventCommand();

        // Act
        var response = await _authenticatedParticipantClient.PostAsJsonAsync("/api/events", eventData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetEvent_ShouldReturnEvent_WhenEventExists()
    {
        // Arrange
        var eventId = await CreateTestEvent();

        // Act
        var response = await _unauthenticatedClient.GetAsync($"/api/events/{eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetEvent_ShouldReturnNotFound_WhenEventDoesNotExist()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();

        // Act
        var response = await _unauthenticatedClient.GetAsync($"/api/events/{nonExistentEventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateEvent_ShouldReturnOk_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var eventId = await CreateTestEvent();
        var updateData = new
        {
            EventId = eventId,
            Event = new
            {
                Title = "Updated Event Name",
                Description = "Updated description",
                StartDateTime = DateTime.UtcNow.AddDays(1),
                EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                Location = "Updated Location",
                Capacity = 100,
                TimeZone = "UTC"
            }
        };

        // Act
        var response = await _authenticatedAdminClient.PutAsJsonAsync($"/api/events/{eventId}", updateData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();
        var eventInDb = await dbContext.Events.FindAsync(eventId);
        eventInDb.Should().NotBeNull();
        eventInDb!.Name.Should().Be("Updated Event Name");
        eventInDb.Capacity.Should().Be(100);
    }

    [Fact]
    public async Task DeleteEvent_ShouldReturnNoContent_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var eventId = await CreateTestEvent();

        // Act
        var response = await _authenticatedAdminClient.DeleteAsync($"/api/events/{eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();
        var eventInDb = await dbContext.Events.FindAsync(eventId);
        eventInDb.Should().BeNull();
    }

    [Fact]
    public async Task RegisterForEvent_ShouldReturnOk_WhenEventHasCapacity()
    {
        // Arrange
        var eventId = await CreateTestEvent();
        var registrationData = TestDataBuilder.Commands.RegisterForEventCommand(eventId, _participantUserId);        

        // Act
        var response = await _authenticatedParticipantClient.PostAsJsonAsync($"/api/events/{eventId}/register", registrationData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterForEvent_ShouldReturnBadRequest_WhenUserAlreadyRegistered()
    {
        // Arrange
        var eventId = await CreateTestEvent();
        var registrationData = TestDataBuilder.Commands.RegisterForEventCommand(eventId, _participantUserId);               

        // Register first time
        await _authenticatedParticipantClient.PostAsJsonAsync($"/api/events/{eventId}/register", registrationData);

        // Act - Try to register again
        var response = await _authenticatedParticipantClient.PostAsJsonAsync($"/api/events/{eventId}/register", registrationData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PublishEvent_ShouldReturnOk_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var eventId = await CreateTestEvent(isPublished: false);

        // Act
        var response = await _authenticatedAdminClient.PostAsync($"/api/events/{eventId}/publish", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();
        var eventInDb = await dbContext.Events.FindAsync(eventId);
        eventInDb.Should().NotBeNull();
        eventInDb!.IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task UnpublishEvent_ShouldReturnOk_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var eventId = await CreateTestEvent();

        // Act
        var response = await _authenticatedAdminClient.PostAsync($"/api/events/{eventId}/unpublish", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();
        var eventInDb = await dbContext.Events.FindAsync(eventId);
        eventInDb.Should().NotBeNull();
        eventInDb!.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task GetEventRegistrations_ShouldReturnRegistrations_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var eventId = await CreateTestEvent();

        // Create a registration first (as participant)
        var registrationData = TestDataBuilder.Commands.RegisterForEventCommand(eventId, _participantUserId);
        await _authenticatedParticipantClient.PostAsJsonAsync($"/api/events/{eventId}/register", registrationData);

        // Act (as admin to view registrations)
        var response = await _authenticatedAdminClient.GetAsync($"/api/events/{eventId}/registrations?pageNumber=1&pageSize=10");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"GetEventRegistrations Response Status: {response.StatusCode}");
        Console.WriteLine($"GetEventRegistrations Response Content: {content}");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetEventRegistrations_ShouldReturnForbidden_WhenAuthenticatedAsParticipant()
    {
        // Arrange
        var eventId = await CreateTestEvent();

        // Act
        var response = await _authenticatedParticipantClient.GetAsync($"/api/events/{eventId}/registrations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateEvent_ShouldReturnBadRequest_WhenNameIsInvalid(string? invalidName)
    {
        // Arrange
        var eventData = TestDataBuilder.Commands.CreateEventCommandWithInvalidData(name: invalidName);

        // Act
        var response = await _authenticatedAdminClient.PostAsJsonAsync("/api/events", eventData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnBadRequest_WhenCapacityIsNegative()
    {
        // Arrange
        var eventData = TestDataBuilder.Commands.CreateEventCommandWithInvalidData(capacity: -1);

        // Act
        var response = await _authenticatedAdminClient.PostAsJsonAsync("/api/events", eventData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnBadRequest_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = startDate.AddHours(-1); // End before start
        var eventData = TestDataBuilder.Commands.CreateEventCommand(
            startDate: startDate,
            endDate: endDate
        );

        // Act
        var response = await _authenticatedAdminClient.PostAsJsonAsync("/api/events", eventData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
} 
