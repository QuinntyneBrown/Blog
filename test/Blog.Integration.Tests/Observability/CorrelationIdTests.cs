using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Observability;

public class CorrelationIdTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CorrelationIdTests(BlogWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task AnyRequest_IncludesCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey("X-Correlation-ID");
        var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
        correlationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Request_WithCorrelationId_EchoesItBack()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-ID", "my-trace-id");

        var response = await _client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-ID").First().Should().Be("my-trace-id");
    }

    [Fact]
    public async Task Request_WithMaliciousCorrelationId_GeneratesNewId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-ID", "<script>alert(1)</script>");

        var response = await _client.SendAsync(request);

        var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
        correlationId.Should().NotBe("<script>alert(1)</script>");
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task EachRequest_GetsDifferentCorrelationId()
    {
        var response1 = await _client.GetAsync("/health");
        var response2 = await _client.GetAsync("/health");

        var id1 = response1.Headers.GetValues("X-Correlation-ID").First();
        var id2 = response2.Headers.GetValues("X-Correlation-ID").First();

        id1.Should().NotBe(id2);
    }
}
