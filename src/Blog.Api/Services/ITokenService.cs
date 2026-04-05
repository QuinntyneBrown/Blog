namespace Blog.Api.Services;

public interface ITokenService
{
    string GenerateToken(Domain.Entities.User user);
    DateTime GetExpiration();
}
