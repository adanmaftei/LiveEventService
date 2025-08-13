using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using LiveEventService.API.Constants;
using LiveEventService.IntegrationTests.Infrastructure;
using Xunit;

namespace LiveEventService.IntegrationTests.Security;

public class SecurityHeadersTests : IClassFixture<LiveEventTestApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityHeadersTests(LiveEventTestApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Endpoint_Has_Security_Headers()
    {
        var response = await _client.GetAsync(RoutePaths.Health);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Headers.Contains(SecurityHeaderNames.XFrameOptions).Should().BeTrue();
        response.Headers.GetValues(SecurityHeaderNames.XFrameOptions).Should().Contain(v => v == "DENY");

        response.Headers.Contains(SecurityHeaderNames.XContentTypeOptions).Should().BeTrue();
        response.Headers.GetValues(SecurityHeaderNames.XContentTypeOptions).Should().Contain(v => v == "nosniff");

        response.Headers.Contains(SecurityHeaderNames.ReferrerPolicy).Should().BeTrue();
        response.Headers.GetValues(SecurityHeaderNames.ReferrerPolicy).Should().Contain(v => v == "strict-origin-when-cross-origin");

        response.Headers.Contains(SecurityHeaderNames.PermissionsPolicy).Should().BeTrue();
    }
}


