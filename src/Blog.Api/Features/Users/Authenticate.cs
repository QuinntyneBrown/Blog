using Blog.Api.Core;
using Blog.Api.Interfaces;
using Blog.Api.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Blog.Api.Features
{
    public class Authenticate
    {
        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(x => x.Username).NotNull();
                RuleFor(x => x.Password).NotNull();
            }
        }

        public record Request(string Username, string Password) : IRequest<Response>;

        public record Response(string AccessToken, Guid UserId);

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly IBlogDbContext _context;
            private readonly IPasswordHasher _passwordHasher;
            private readonly ITokenBuilder _tokenBuilder;
            private readonly IOrchestrationHandler _orchestrationHandler;

            public Handler(IBlogDbContext context, IPasswordHasher passwordHasher, ITokenBuilder tokenBuilder, IOrchestrationHandler orchestrationHandler)
            {
                _context = context;
                _passwordHasher = passwordHasher;
                _tokenBuilder = tokenBuilder;
                _orchestrationHandler = orchestrationHandler;
            }

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                var user = await _context.Users
                    .SingleOrDefaultAsync(x => x.Username == request.Username);

                if (user == null)
                    throw new Exception();

                if (!ValidateUser(user, _passwordHasher.HashPassword(user.Salt, request.Password)))
                    throw new Exception();

                var token = _tokenBuilder
                    .AddUsername(user.Username)
                    .AddClaim(new System.Security.Claims.Claim(Constants.ClaimTypes.UserId, $"{user.UserId}"))
                    .AddClaim(new System.Security.Claims.Claim(Constants.ClaimTypes.Username, $"{user.Username}"))
                    .Build();

                return new(token, user.UserId);
            }

            public bool ValidateUser(User user, string transformedPassword)
            {
                if (user == null || transformedPassword == null)
                    return false;

                return user.Password == transformedPassword;
            }
        }
    }
}
