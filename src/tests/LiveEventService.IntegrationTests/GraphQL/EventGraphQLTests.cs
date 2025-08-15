using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LiveEventService.IntegrationTests.Infrastructure;
using LiveEventService.Infrastructure.Data;

namespace LiveEventService.IntegrationTests.GraphQL;

public class EventGraphQLTests : BaseLiveEventsTests
{
    public EventGraphQLTests(LiveEventTestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetEvents_ShouldReturnPublishedEvents_WhenUnauthenticated()
    {
        // Arrange
        var eventId = await CreateTestEvent();
        var query = TestDataBuilder.GraphQL.GetEventsQuery(isPublished: true, pageSize: 10);

        // Act
        var response = await ExecuteGraphQLQuery(_unauthenticatedClient, query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("events");
        content.Should().Contain("items");
        content.Should().NotContain("errors");
    }

    [Fact]
    public async Task GetEvents_ShouldReturnAllEvents_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var eventId = await CreateTestEvent();
        var query = TestDataBuilder.GraphQL.GetEventsQuery(pageSize: 10);

        // Act
        var response = await ExecuteGraphQLQuery(_authenticatedAdminClient, query);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Status: {response.StatusCode}");
        Console.WriteLine($"Response Content: {content}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("events");
        content.Should().Contain("items");
        content.Should().NotContain("errors");
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnSuccess_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var mutation = TestDataBuilder.GraphQL.CreateEventMutation(
            title: "GraphQL Test Event",
            capacity: 75
        );

        // Log the exact mutation being sent
        Console.WriteLine($"Mutation being sent:");
        Console.WriteLine(mutation);

        // Act
        var response = await ExecuteGraphQLQuery(_authenticatedAdminClient, mutation);

        // Assert (temporarily log the response for debugging)
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Status: {response.StatusCode}");
        Console.WriteLine($"Response Content: {content}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("createEvent");
        content.Should().Contain("id");
        content.Should().NotContain("errors");

        // Verify in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();
        var eventInDb = await dbContext.Events.FirstOrDefaultAsync(e => e.Name == "GraphQL Test Event");
        eventInDb.Should().NotBeNull();
        eventInDb!.Capacity.Should().Be(75);
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnError_WhenUnauthenticated()
    {
        // Arrange
        var mutation = TestDataBuilder.GraphQL.CreateEventMutation();

        // Act
        var response = await ExecuteGraphQLQuery(_unauthenticatedClient, mutation);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Unauthenticated Response Status: {response.StatusCode}");
        Console.WriteLine($"Unauthenticated Response Content: {content}");

        response.StatusCode.Should().Be(HttpStatusCode.OK); // GraphQL returns 200 with errors in content
        content.Should().Contain("errors");
        content.Should().NotContain("\"data\":{\"createEvent\""); // Should not contain successful data
    }

    [Fact]
    public async Task CreateUser_ShouldReturnSuccess_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var mutation = TestDataBuilder.GraphQL.CreateUserMutation(
            email: "graphql-test@example.com",
            firstName: "GraphQL",
            lastName: "User"
        );

        // Act
        var response = await ExecuteGraphQLQuery(_authenticatedAdminClient, mutation);

        // Assert (temporarily log the response for debugging)
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Status: {response.StatusCode}");
        Console.WriteLine($"Response Content: {content}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("createUser");
        content.Should().Contain("id");
        content.Should().NotContain("errors");

        // Verify in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();
        var userInDb = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "graphql-test@example.com");
        userInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterForEvent_ShouldReturnSuccess_WhenEventHasCapacity()
    {
        // Arrange
        var eventId = await CreateTestEvent();
        var mutation = TestDataBuilder.GraphQL.RegisterForEventMutation(eventId, _participantUserId);

        // Act
        var response = await ExecuteGraphQLQuery(_authenticatedParticipantClient, mutation);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("registerForEvent");
        content.Should().Contain("id");
        content.Should().NotContain("errors");

        // Verify in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();
        var registration = await dbContext.EventRegistrations
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.User.IdentityId == _participantUserId);
        registration.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterForEvent_ShouldReturnWaitlisted_WhenEventIsFull()
    {
        // Arrange
        var eventId = await CreateFullEvent();
        var mutation = TestDataBuilder.GraphQL.RegisterForEventMutation(eventId, _participantUserId);

        // Act
        var response = await ExecuteGraphQLQuery(_authenticatedParticipantClient, mutation);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("registerForEvent");
        content.Should().Contain("id");
        content.Should().Contain("positionInQueue");
    }

    [Fact]
    public async Task GetEvents_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        await SeedMultipleEvents(5);
        var query = @"
            query {
                events(pageSize: 2) {
                    items {
                        id
                        title
                    }
                    totalCount
                    pageNumber
                    pageSize
                    totalPages
                }
            }";

        // Act
        var response = await ExecuteGraphQLQuery(_authenticatedAdminClient, query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalCount");
        content.Should().Contain("pageSize");
    }

    [Fact]
    public async Task CreateEvent_WithInvalidInput_ShouldReturnValidationErrors()
    {
        // Arrange
        var mutation = @"
            mutation {
                createEvent(input: {
                    title: """",
                    description: ""Test Event"",
                    startDateTime: ""2024-01-01T10:00:00Z"",
                    endDateTime: ""2024-01-01T09:00:00Z"",
                    location: ""Test Location"",
                    capacity: -1
                }) {
                    id
                    title
                    description
                }
            }";

        // Act
        var response = await ExecuteGraphQLQuery(_authenticatedAdminClient, mutation);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest); // GraphQL returns 400 for validation errors
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("errors");
    }

    [Fact]
    public async Task GraphQL_Query_ShouldHandleComplexFiltering()
    {
        // Arrange
        await SeedEventsWithDifferentDates();
        var query = @"
            query {
                events(
                    isPublished: true,
                    pageSize: 10
                ) {
                    items {
                        id
                        title
                        startDateTime
                        isPublished
                    }
                    totalCount
                }
            }";

        // Act
        var response = await ExecuteGraphQLQuery(_authenticatedAdminClient, query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("events");
        content.Should().NotContain("errors");
    }

    private new async Task<HttpResponseMessage> ExecuteGraphQLQuery(HttpClient client, string query)
    {
        var request = new
        {
            query = query
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // The client should already have the authorization header set if it's authenticated
        return await client.PostAsync("/graphql", content);
    }
}
