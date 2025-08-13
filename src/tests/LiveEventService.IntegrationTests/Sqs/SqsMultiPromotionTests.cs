using System.Linq;
using System.Net;
using System.Net.Http.Json;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.EventRegistration;
using LiveEventService.IntegrationTests.Infrastructure.Sqs;
using Xunit;
using LiveEventService.Application.Features.Events.Event;

namespace LiveEventService.IntegrationTests.Sqs;

public class SqsMultiPromotionTests : IClassFixture<SqsTestApplicationFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _participant;
    private readonly HttpClient _third;

    public SqsMultiPromotionTests(SqsTestApplicationFactory factory)
    {
        _admin = factory.CreateClient();
        _admin.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "admin-user|Admin|admin@test.com");

        _participant = factory.CreateClient();
        _participant.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "participant-user|Participant|participant@test.com");

        _third = factory.CreateClient();
        _third.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "user3|Participant|user3@test.com");
    }

    [Fact]
    public async Task Two_Waitlisted_Users_Promote_In_Order_On_Two_Cancellations()
    {
        // Create small event: capacity 1
        var createPayload = new { Title = "SQS Multi Promotion", Description = "test", StartDateTime = DateTime.UtcNow.AddHours(1), EndDateTime = DateTime.UtcNow.AddHours(2), TimeZone = "UTC", Location = "Somewhere", Capacity = 1 };
        var createResp = await _admin.PostAsJsonAsync("/api/events", new { Event = createPayload });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<BaseResponse<EventDto>>();
        var eventId = created!.Data!.Id;

        // Publish
        (await _admin.PostAsync($"/api/events/{eventId}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Three registrations: participant confirmed, admin waitlisted, third waitlisted
        (await _participant.PostAsJsonAsync($"/api/events/{eventId}/register", new { })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _admin.PostAsJsonAsync($"/api/events/{eventId}/register", new { })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _third.PostAsJsonAsync($"/api/events/{eventId}/register", new { })).StatusCode.Should().Be(HttpStatusCode.OK);

        // Snapshot: confirm participant is confirmed; admin then user3 are waitlisted in order
        var list1 = await _admin.GetFromJsonAsync<BaseResponse<EventRegistrationListDto>>($"/api/events/{eventId}/registrations");
        list1!.Success.Should().BeTrue();
        list1.Data!.Items.First(r => r.Status == "Confirmed").UserEmail.Should().Be("participant@test.com");
        var waitlisted = list1.Data!.Items.Where(r => r.Status == "Waitlisted").OrderBy(r => r.PositionInQueue).ToList();
        waitlisted.Select(w => w.UserEmail).Should().ContainInOrder("admin@test.com", "user3@test.com");

        // Cancel confirmed (participant) -> admin should promote
        var confirmedId = list1.Data!.Items.First(r => r.Status == "Confirmed").Id;
        (await _admin.PostAsync($"/api/events/{eventId}/registrations/{confirmedId}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(1000);

        var list2 = await _admin.GetFromJsonAsync<BaseResponse<EventRegistrationListDto>>($"/api/events/{eventId}/registrations");
        list2!.Success.Should().BeTrue();
        list2.Data!.Items.First(r => r.Status == "Confirmed").UserEmail.Should().Be("admin@test.com");
        var waitlisted2 = list2.Data!.Items.Where(r => r.Status == "Waitlisted").OrderBy(r => r.PositionInQueue).ToList();
        waitlisted2.Select(w => w.UserEmail).Should().ContainSingle().Which.Should().Be("user3@test.com");

        // Cancel new confirmed (admin) -> user3 should promote
        var confirmedAdminId = list2.Data!.Items.First(r => r.Status == "Confirmed").Id;
        (await _admin.PostAsync($"/api/events/{eventId}/registrations/{confirmedAdminId}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(1000);

        var list3 = await _admin.GetFromJsonAsync<BaseResponse<EventRegistrationListDto>>($"/api/events/{eventId}/registrations");
        list3!.Success.Should().BeTrue();
        list3.Data!.Items.First(r => r.Status == "Confirmed").UserEmail.Should().Be("user3@test.com");
        list3.Data!.Items.Count(r => r.Status == "Waitlisted").Should().Be(0);
    }
}


