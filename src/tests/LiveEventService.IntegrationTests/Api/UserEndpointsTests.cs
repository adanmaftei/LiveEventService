using System.Net;
using System.Net.Http.Json;
using LiveEventService.IntegrationTests.Infrastructure;
using LiveEventService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LiveEventService.IntegrationTests.Api;

public class UserEndpointsTests : BaseLiveEventsTests
{
    public UserEndpointsTests(LiveEventTestApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ExportUserData_ShouldReturnJson_WhenRequestedBySelf()
    {
        // Arrange: ensure participant exists (Base sets it up)
        var userId = _participantUserId;
        var client = _factory.CreateAuthenticatedClient(userId, "Participant", "participant@test.com");

        // Act
        var response = await client.GetAsync($"/api/users/{userId}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("IdentityId");
    }

    [Fact]
    public async Task ExportUserData_ShouldReturnForbidden_WhenRequestedByDifferentUser()
    {
        // Arrange: requester is a different participant
        var otherUserId = Guid.NewGuid().ToString();
        var requester = _factory.CreateAuthenticatedClient(otherUserId, "Participant", "other@test.com");

        // Act
        var response = await requester.GetAsync($"/api/users/{_participantUserId}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EraseUser_ShouldAnonymizeAndDeactivate_WhenCalledByAdmin()
    {
        // Arrange: create a disposable user
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();
        var identityId = Guid.NewGuid().ToString();
        var user = TestDataBuilder.CreateUser(identityId: identityId, email: "erase@test.com", firstName: "To", lastName: "Erase");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Act
        var response = await _authenticatedAdminClient.DeleteAsync($"/api/users/{user.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify in DB
        var reloaded = await db.Users.IgnoreQueryFilters().AsNoTracking().FirstAsync(u => u.Id == user.Id);
        reloaded.IsActive.Should().BeFalse();
        reloaded.Email.Should().Contain("anon+");
        reloaded.FirstName.Should().BeEmpty();
        reloaded.LastName.Should().BeEmpty();
        reloaded.PhoneNumber.Should().BeEmpty();
    }
}


