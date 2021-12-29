using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Blog.Api.Core;
using Blog.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features
{
    public class GetPosts
    {
        public class Request: IRequest<Response> { }

        public class Response: ResponseBase
        {
            public List<PostDto> Posts { get; set; }
        }

        public class Handler: IRequestHandler<Request, Response>
        {
            private readonly IBlogDbContext _context;
        
            public Handler(IBlogDbContext context)
                => _context = context;
        
            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                return new () {
                    Posts = await _context.Posts.Select(x => x.ToDto()).ToListAsync()
                };
            }
            
        }
    }
}
