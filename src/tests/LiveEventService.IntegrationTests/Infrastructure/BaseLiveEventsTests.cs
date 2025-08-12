using System.Net;
using System.Net.Http.Json;
using System.Text;
using LiveEventService.Application.Features.Events.EventRegistration;
using LiveEventService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LiveEventService.IntegrationTests.Infrastructure;
public abstract class BaseLiveEventsTests : IClassFixture<LiveEventTestApplicationFactory>, IDisposable
{
    protected readonly LiveEventTestApplicationFactory _factory;
    protected readonly HttpClient _authenticatedAdminClient;
    protected readonly HttpClient _authenticatedParticipantClient;
    protected readonly HttpClient _unauthenticatedClient;

    protected readonly string _adminUserId = "admin-user";
    protected readonly string _participantUserId = "participant-user";

    protected BaseLiveEventsTests(LiveEventTestApplicationFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _authenticatedAdminClient = _factory.CreateAuthenticatedClient("admin-user", "Admin", "admin@test.com");
        _authenticatedParticipantClient = _factory.CreateAuthenticatedClient("participant-user", "Participant", "participant@test.com");
        _unauthenticatedClient = _factory.CreateClient();
        
        // Ensure the authenticated users exist in the database
        EnsureAuthenticatedUsersExist().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Ensures that the users required for authenticated clients exist in the database
    /// </summary>
    private async Task EnsureAuthenticatedUsersExist()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();

        // Check if admin user exists, create if not
        var adminExists = await dbContext.Users.AnyAsync(u => u.IdentityId == "admin-user");
        if (!adminExists)
        {
            var adminUser = TestDataBuilder.CreateUser("admin-user", "admin@test.com", "Admin", "User");
            dbContext.Users.Add(adminUser);
        }

        // Check if participant user exists, create if not
        var participantExists = await dbContext.Users.AnyAsync(u => u.IdentityId == "participant-user");
        if (!participantExists)
        {
            var participantUser = TestDataBuilder.CreateUser("participant-user", "participant@test.com", "Participant", "User");
            dbContext.Users.Add(participantUser);
        }

        await dbContext.SaveChangesAsync();
    }

    protected async Task<Guid> CreateTestEvent(bool isPublished = true)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();

        // Create test event
        var testEvent = TestDataBuilder.CreateEvent(
            name: "Integration Test Event",
            capacity: 100,
            isPublished: isPublished
        );
        dbContext.Events.Add(testEvent);

        await dbContext.SaveChangesAsync();

        return testEvent.Id;
    }

    protected async Task<Guid> CreateFullEvent()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();

        // Create users - first 5 for filling event, last one is our participant user
        var users = TestDataBuilder.CreateUsersList(5); // 5 for filling event        
        dbContext.Users.AddRange(users);        

        // Create event with capacity of 5
        var testEvent = TestDataBuilder.CreateEvent(capacity: 5, isPublished: true);
        dbContext.Events.Add(testEvent);

        // Fill the event to capacity
        for (int i = 0; i < 5; i++)
        {
            var registration = TestDataBuilder.CreateEventRegistration(testEvent, users[i]);
            dbContext.EventRegistrations.Add(registration);
        }

        await dbContext.SaveChangesAsync();

        return testEvent.Id;
    }

    protected async Task SeedMultipleEvents(int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();

        var events = TestDataBuilder.CreateEventsList(count, false); // All published
        dbContext.Events.AddRange(events);
        await dbContext.SaveChangesAsync();
    }

    protected async Task SeedEventsWithDifferentDates()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();

        var events = new[]
        {
            TestDataBuilder.CreateEvent(
                startDate: new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                isPublished: true,
                organizerId: "admin-user"
            ),
            TestDataBuilder.CreateEvent(
                startDate: new DateTime(2023, 12, 15, 10, 0, 0, DateTimeKind.Utc),
                isPublished: true,
                organizerId: "admin-user"
            ),
            TestDataBuilder.CreateEvent(
                startDate: new DateTime(2024, 2, 15, 10, 0, 0, DateTimeKind.Utc),
                isPublished: false,
                organizerId: "admin-user"
            )
        };

        dbContext.Events.AddRange(events);
        await dbContext.SaveChangesAsync();
    }

    protected async Task<Guid> CreateEventWithCapacity(int capacity)
    {
        var eventData = TestDataBuilder.Commands.CreateEventCommand(
            name: $"Test Event - Capacity {capacity}",
            capacity: capacity);

        var response = await _authenticatedAdminClient.PostAsJsonAsync("/api/events", eventData);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Parse the response properly
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);
        var dataElement = jsonDoc.RootElement.GetProperty("data");
        var idElement = dataElement.GetProperty("id");
        var eventId = Guid.Parse(idElement.GetString()!);

        // Publish the event so registrations are accepted
        var publishResponse = await _authenticatedAdminClient.PostAsync($"/api/events/{eventId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return eventId;
    }

    protected async Task<Guid> CreateUserAndRegisterForEvent(Guid eventId, string email, string firstName, string lastName)
    {
        // Create user directly in database with a unique identity ID
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();

        var identityId = Guid.NewGuid().ToString();
        var user = TestDataBuilder.CreateUser(
            identityId: identityId,
            email: email,
            firstName: firstName,
            lastName: lastName
        );

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Create a new authenticated client for this user
        var userClient = _factory.CreateAuthenticatedClient(identityId, "Participant", email);

        // Register for the event using the proper command structure
        var registrationData = TestDataBuilder.Commands.RegisterForEventCommand(eventId, identityId);
        var registrationResponse = await userClient.PostAsJsonAsync($"/api/events/{eventId}/register", registrationData);
        registrationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return user.Id;
    }

    protected async Task<List<EventRegistrationDto>> GetEventRegistrations(Guid eventId)
    {
        var response = await _authenticatedAdminClient.GetAsync($"/api/events/{eventId}/registrations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // The API returns BaseResponse<EventRegistrationListDto>
        var dataElement = jsonDoc.RootElement.GetProperty("data");
        var itemsElement = dataElement.GetProperty("items");

        // Use proper JSON options for case-insensitive property matching
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var registrations = JsonSerializer.Deserialize<List<EventRegistrationDto>>(itemsElement.ToString(), options);
        return registrations ?? new List<EventRegistrationDto>();
    }

    protected async Task<List<EventRegistrationDto>> GetEventRegistrationsByStatus(Guid eventId, string status)
    {
        var response = await _authenticatedAdminClient.GetAsync($"/api/events/{eventId}/registrations?status={status}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // The API returns BaseResponse<EventRegistrationListDto>
        var dataElement = jsonDoc.RootElement.GetProperty("data");
        var itemsElement = dataElement.GetProperty("items");

        // Use proper JSON options for case-insensitive property matching
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var registrations = JsonSerializer.Deserialize<List<EventRegistrationDto>>(itemsElement.ToString(), options);
        return registrations ?? new List<EventRegistrationDto>();
    }

    protected async Task<HttpResponseMessage> ExecuteGraphQLQuery(HttpClient client, string query)
    {
        var graphqlRequest = new
        {
            query = query,
            variables = new { }
        };

        var json = JsonSerializer.Serialize(graphqlRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await client.PostAsync("/graphql", content);
    }

    private async Task CleanupTestData()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();

        // Clean up test data
        dbContext.EventRegistrations.RemoveRange(dbContext.EventRegistrations);
        dbContext.Events.RemoveRange(dbContext.Events);
        dbContext.Users.RemoveRange(dbContext.Users);
        await dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        // Clean up after each test
        try
        {
            CleanupTestData().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // Log the exception if needed, but don't throw during disposal
            Console.WriteLine($"Error during test cleanup: {ex.Message}");
        }
    }
}
