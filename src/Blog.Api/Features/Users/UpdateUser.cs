using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Blog.Api.Core;
using Blog.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features
{
    public class UpdateUser
    {
        public class Validator: AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(request => request.User).NotNull();
                RuleFor(request => request.User).SetValidator(new UserValidator());
            }
        
        }

        public class Request: IRequest<Response>
        {
            public UserDto User { get; set; }
        }

        public class Response: ResponseBase
        {
            public UserDto User { get; set; }
        }

        public class Handler: IRequestHandler<Request, Response>
        {
            private readonly IBlogDbContext _context;
        
            public Handler(IBlogDbContext context)
                => _context = context;
        
            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                var user = await _context.Users.SingleAsync(x => x.UserId == request.User.UserId);
                
                await _context.SaveChangesAsync(cancellationToken);
                
                return new Response()
                {
                    User = user.ToDto()
                };
            }
            
        }
    }
}
