namespace Blog.Api.Services;

public interface IUnsubscribeTokenService
{
    string GenerateToken(Guid subscriberId);
    Guid? ValidateAndExtractSubscriberId(string token);
}
