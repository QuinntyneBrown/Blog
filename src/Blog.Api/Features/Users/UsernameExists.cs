using Blog.Api.Core;
using Blog.Api.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Blog.Api.Features
{
    public class UsernameExists
    {
        public class Request : IRequest<Response>
        {
            public string Username { get; set; }
        }

        public class Response : ResponseBase
        {
            public bool Exists { get; set; }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly IBlogDbContext _context;

            public Handler(IBlogDbContext context)
                => _context = context;

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                return new()
                {
                    Exists = (await _context.Users.SingleOrDefaultAsync(x => x.Username == request.Username)) != null
                };
            }

        }
    }
}
