using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Auth;

public class LoginTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var response = await _client.PostAsync("/api/auth/login",
            JsonBody(new { email = "admin@blog.dev", password = "Admin1234!" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        data.GetProperty("expiresAt").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        var response = await _client.PostAsync("/api/auth/login",
            JsonBody(new { email = "admin@blog.dev", password = "WrongPassword123" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        var response = await _client.PostAsync("/api/auth/login",
            JsonBody(new { email = "nobody@example.com", password = "SomePassword1" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_MissingEmail_Returns400()
    {
        var response = await _client.PostAsync("/api/auth/login",
            JsonBody(new { email = "", password = "Admin1234!" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_MissingPassword_Returns400()
    {
        var response = await _client.PostAsync("/api/auth/login",
            JsonBody(new { email = "admin@blog.dev", password = "" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_Returns400()
    {
        var response = await _client.PostAsync("/api/auth/login",
            JsonBody(new { email = "not-an-email", password = "Admin1234!" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_PasswordTooShort_Returns400()
    {
        var response = await _client.PostAsync("/api/auth/login",
            JsonBody(new { email = "admin@blog.dev", password = "short" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401WithProblemDetail()
    {
        var response = await _client.PostAsync("/api/auth/login",
            JsonBody(new { email = "admin@blog.dev", password = "WrongPassword123" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("detail").GetString()
            .Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401WithSameGenericMessage()
    {
        var response = await _client.PostAsync("/api/auth/login",
            JsonBody(new { email = "nobody@example.com", password = "SomePassword1" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("detail").GetString()
            .Should().Be("Invalid email or password.");
    }
}
