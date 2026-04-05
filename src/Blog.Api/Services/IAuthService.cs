using Blog.Api.Features.Auth.Commands;

namespace Blog.Api.Services;

/// <summary>
/// Orchestrates the login workflow — looks up the user, verifies the password,
/// and triggers token generation.
/// Returns a <see cref="LoginResponse"/> DTO on success or throws an authentication
/// exception on failure. Updates <c>LastLoginAt</c> on the user record.
/// </summary>
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct);
}
