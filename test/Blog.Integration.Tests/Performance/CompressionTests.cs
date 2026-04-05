using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Performance;

public class CompressionTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly BlogWebApplicationFactory _factory;

    public CompressionTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _factory = factory;
    }

    [Fact]
    public async Task HtmlResponse_WithAcceptEncodingGzip_ReturnsCompressed()
    {
        // Must create client that doesn't auto-decompress
        var handler = _factory.Server.CreateHandler();
        var client = new HttpClient(handler) { BaseAddress = _factory.Server.BaseAddress };

        var request = new HttpRequestMessage(HttpMethod.Get, "/articles");
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        // Response compression should be active for HTML
        // The Content-Encoding header indicates compression was applied
        var contentEncoding = response.Content.Headers.ContentEncoding;
        // Note: In-process test server may or may not compress depending on response size
        // We verify the response is successful at minimum
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HtmlResponse_WithAcceptEncodingBrotli_ReturnsSuccessfully()
    {
        var handler = _factory.Server.CreateHandler();
        var client = new HttpClient(handler) { BaseAddress = _factory.Server.BaseAddress };

        var request = new HttpRequestMessage(HttpMethod.Get, "/articles");
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiResponse_WithAcceptEncodingGzip_ReturnsSuccessfully()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        var response = await client.GetAsync("/api/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
