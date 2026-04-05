using Blog.Api.Common.Exceptions;
using Blog.Api.Features.Auth.Commands;
using Blog.Domain.Interfaces;

namespace Blog.Api.Services;

/// <summary>
/// Orchestrates the login workflow — looks up the user, verifies the password,
/// and triggers token generation. Updates <c>LastLoginAt</c> on the user record.
/// Design reference: Section 3.2 — AuthService.
/// </summary>
public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IEmailRateLimitService emailRateLimitService,
    IUnitOfWork uow,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct)
    {
        if (!emailRateLimitService.TryAcquire(email, out var retryAfterSeconds))
            throw new RateLimitExceededException(
                "Too many login attempts for this email address. Please try again later.",
                retryAfterSeconds);

        var user = await userRepository.GetByEmailAsync(email, ct);
        if (user == null)
        {
            logger.LogInformation("Business event {EventType} occurred: {@Details}",
                "UserAuthenticationFailed", new { Reason = "UserNotFound" });
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            logger.LogInformation("Business event {EventType} occurred: {@Details}",
                "UserAuthenticationFailed", new { Reason = "InvalidPassword", UserId = user.UserId });
            throw new UnauthorizedException("Invalid email or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        userRepository.Update(user);

        await uow.SaveChangesAsync(ct);

        var token = tokenService.GenerateToken(user);
        var expiresAt = tokenService.GetExpiration();

        logger.LogInformation("Business event {EventType} occurred: {@Details}",
            "UserAuthenticated", new { UserId = user.UserId });

        return new LoginResponse(token, expiresAt);
    }
}
