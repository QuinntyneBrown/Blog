using System.Net;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Pages;

public class PageStructureTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PageStructureTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AboutPage_EmptyState_ReturnsHtmlWithEmptyStructure()
    {
        var response = await _client.GetAsync("/about");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("about-empty");
        html.Should().Contain("Content coming soon.");
    }

    [Fact]
    public async Task NewsletterPage_Returns200WithHeroAndArchive()
    {
        var response = await _client.GetAsync("/newsletter");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("newsletter-hero");
        html.Should().Contain("Stay in the loop");
        html.Should().Contain("newsletter-archive");
        html.Should().Contain("Past Issues");
    }

    [Fact]
    public async Task EventsPage_Returns200WithHeroAndSections()
    {
        var response = await _client.GetAsync("/events");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("events-hero");
        html.Should().Contain("Speaking &amp; Appearances");
        html.Should().Contain("events-section");
        html.Should().Contain("Upcoming");
        html.Should().Contain("Past Events");
    }

    [Theory]
    [InlineData("/about")]
    [InlineData("/newsletter")]
    [InlineData("/events")]
    public async Task NavLinks_ContainNewPages(string path)
    {
        var response = await _client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("href=\"/newsletter\"");
        html.Should().Contain("href=\"/events\"");
    }
}
