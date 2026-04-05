using Xunit;
using FluentAssertions;

namespace Blog.Integration.Tests;

/// <summary>
/// Integration tests that start the real application against an in-memory or test database.
/// Expand as API and Web projects are wired up.
/// </summary>
public class ApiIntegrationTests
{
    [Fact]
    public void Placeholder_IntegrationTestsWillTargetRealHttpEndpoints()
    {
        // Integration tests will use WebApplicationFactory<Program> once
        // Blog.Api is updated with <InternalsVisibleTo> and test entry point.
        true.Should().BeTrue();
    }
}
