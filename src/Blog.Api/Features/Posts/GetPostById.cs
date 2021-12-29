using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Blog.Api.Core;
using Blog.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features
{
    public class GetPostById
    {
        public class Request: IRequest<Response>
        {
            public Guid PostId { get; set; }
        }

        public class Response: ResponseBase
        {
            public PostDto Post { get; set; }
        }

        public class Handler: IRequestHandler<Request, Response>
        {
            private readonly IBlogDbContext _context;
        
            public Handler(IBlogDbContext context)
                => _context = context;
        
            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                return new () {
                    Post = (await _context.Posts.SingleOrDefaultAsync(x => x.PostId == request.PostId)).ToDto()
                };
            }
            
        }
    }
}
