using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Security;

public class CspReportTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CspReportTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CspReport_ValidPayload_Returns204()
    {
        var report = """{"csp-report":{"document-uri":"https://example.com","violated-directive":"script-src 'self'"}}""";
        var content = new StringContent(report, Encoding.UTF8, "application/csp-report");

        var response = await _client.PostAsync("/api/csp-report", content);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CspReport_JsonPayload_Returns204()
    {
        var report = """{"type":"csp-violation","body":{"documentURL":"https://example.com"}}""";
        var content = new StringContent(report, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/csp-report", content);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CspReport_ReportsJsonPayload_Returns204()
    {
        var report = """[{"type":"csp-violation","age":0,"url":"https://example.com","body":{}}]""";
        var content = new StringContent(report, Encoding.UTF8, "application/reports+json");

        var response = await _client.PostAsync("/api/csp-report", content);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CspReport_DoesNotRequireAuthentication()
    {
        var report = """{"csp-report":{"document-uri":"https://example.com"}}""";
        var content = new StringContent(report, Encoding.UTF8, "application/csp-report");

        // Using a client without auth credentials
        var response = await _client.PostAsync("/api/csp-report", content);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SecurityHeaders_ContainCspReportDirectives()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        csp.Should().Contain("report-uri /api/csp-report");
        csp.Should().Contain("report-to csp-endpoint");
    }

    [Fact]
    public async Task SecurityHeaders_ContainReportingEndpointsHeader()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        response.Headers.Contains("Reporting-Endpoints").Should().BeTrue();
        var reportingEndpoints = response.Headers.GetValues("Reporting-Endpoints").First();
        reportingEndpoints.Should().Contain("csp-endpoint=\"/api/csp-report\"");
    }
}
