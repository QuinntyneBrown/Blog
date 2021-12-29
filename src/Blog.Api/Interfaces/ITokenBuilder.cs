using System.Security.Claims;

namespace Blog.Api.Interfaces
{
    public interface ITokenBuilder
    {
        ITokenBuilder AddOrUpdateClaim(Claim claim);
        ITokenBuilder AddClaim(Claim claim);
        ITokenBuilder AddUsername(string username);
        string Build();
        ITokenBuilder FromClaimsPrincipal(ClaimsPrincipal claimsPrincipal);
        ITokenBuilder RemoveClaim(Claim claim);
    }
}
