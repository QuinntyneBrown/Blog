using Blog.Domain.Interfaces;
using Blog.Api.Common.Exceptions;
using Blog.Api.Common.Models;

using Blog.Api.Services;
using FluentValidation;
using MediatR;

namespace Blog.Api.Features.Auth.Commands;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt);

public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IEmailRateLimitService emailRateLimitService,
    IUnitOfWork uow,
    ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (!emailRateLimitService.TryAcquire(request.Email))
            throw new RateLimitExceededException("Too many login attempts for this email address. Please try again later.");

        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            logger.LogInformation("Business event {EventType} occurred: {@Details}",
                "UserAuthenticationFailed", new { Reason = "UserNotFound" });
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            logger.LogInformation("Business event {EventType} occurred: {@Details}",
                "UserAuthenticationFailed", new { Reason = "InvalidPassword", UserId = user.UserId });
            throw new UnauthorizedException("Invalid email or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        userRepository.Update(user);

        await uow.SaveChangesAsync(cancellationToken);

        var token = tokenService.GenerateToken(user);
        var expiresAt = tokenService.GetExpiration();

        logger.LogInformation("Business event {EventType} occurred: {@Details}",
            "UserAuthenticated", new { UserId = user.UserId });

        return new LoginResponse(token, expiresAt);
    }
}
