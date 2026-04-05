using Xunit;
using Blog.Domain.Entities;
using FluentAssertions;

namespace Blog.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void User_DefaultValues_AreCorrect()
    {
        var user = new User();

        user.LastLoginAt.Should().BeNull();
        user.DigitalAssets.Should().BeEmpty();
    }

    [Fact]
    public void User_CanSetProperties()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var user = new User
        {
            UserId = id,
            Email = "admin@blog.dev",
            DisplayName = "Quinn Brown",
            PasswordHash = "hashed",
            CreatedAt = now,
            LastLoginAt = now
        };

        user.UserId.Should().Be(id);
        user.Email.Should().Be("admin@blog.dev");
        user.LastLoginAt.Should().Be(now);
    }
}
