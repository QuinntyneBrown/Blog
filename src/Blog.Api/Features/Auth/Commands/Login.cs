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

public class LoginCommandHandler(IAuthService authService) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await authService.LoginAsync(request.Email, request.Password, cancellationToken);
    }
}
