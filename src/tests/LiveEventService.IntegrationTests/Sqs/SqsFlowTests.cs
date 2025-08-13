using System.Net;
using System.Net.Http.Json;
using LiveEventService.IntegrationTests.Infrastructure.Sqs;
using Xunit;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.EventRegistration;
using System.Text.Json;
using System.Linq;

namespace LiveEventService.IntegrationTests.Sqs;

public class SqsFlowTests : IClassFixture<SqsTestApplicationFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _participant;

    public SqsFlowTests(SqsTestApplicationFactory factory)
    {
        _admin = factory.CreateClient();
        _admin.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "admin-user|Admin|admin@test.com");
        _participant = factory.CreateClient();
        _participant.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "participant-user|Participant|participant@test.com");
    }

    [Fact]
    public async Task DomainEvents_Are_Processed_Via_SQS_Path()
    {
        // Create event
        var createPayload = new
        {
            Title = "SQS Flow Event",
            Description = "test",
            StartDateTime = DateTime.UtcNow.AddHours(1),
            EndDateTime = DateTime.UtcNow.AddHours(2),
            TimeZone = "UTC",
            Location = "Somewhere",
            Capacity = 1
        };
        var createResp = await _admin.PostAsJsonAsync("/api/events", new { Event = createPayload });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(createJson);
        var idStr = doc.RootElement.GetProperty("data").GetProperty("id").GetString();
        Guid eventId = Guid.Parse(idStr!);

        // Publish
        var publishResp = await _admin.PostAsync($"/api/events/{eventId}/publish", null);
        publishResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Two registrations (different users): one confirmed, one waitlisted -> triggers domain events handled via SQS
        var reg1 = await _participant.PostAsJsonAsync($"/api/events/{eventId}/register", new { });
        reg1.StatusCode.Should().Be(HttpStatusCode.OK);
        var reg2 = await _admin.PostAsJsonAsync($"/api/events/{eventId}/register", new { });
        reg2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cancel confirmed to trigger promotion via SQS
        var list = await _admin.GetFromJsonAsync<BaseResponse<EventRegistrationListDto>>($"/api/events/{eventId}/registrations");
        list!.Success.Should().BeTrue();
        var confirmed = list.Data!.Items.First(r => r.Status == "Confirmed");
        // Sanity: confirmed should be the participant (registered first)
        confirmed.UserEmail.Should().Be("participant@test.com");
        Guid confirmedId = confirmed.Id;
        var cancel = await _admin.PostAsync($"/api/events/{eventId}/registrations/{confirmedId}/cancel", null);
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);

        // Allow SQS worker to process
        await Task.Delay(1000);

        // Validate state reflects promotion: admin should be promoted to Confirmed
        var updated = await _admin.GetFromJsonAsync<BaseResponse<EventRegistrationListDto>>($"/api/events/{eventId}/registrations");
        updated!.Success.Should().BeTrue();
        updated.Data!.Items.Count(r => r.Status == "Confirmed").Should().Be(1);
        updated.Data!.Items.First(r => r.Status == "Confirmed").UserEmail.Should().Be("admin@test.com");
    }
}


