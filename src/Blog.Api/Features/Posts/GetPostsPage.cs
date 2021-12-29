using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Blog.Api.Extensions;
using Blog.Api.Core;
using Blog.Api.Interfaces;
using Blog.Api.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features
{
    public class GetPostsPage
    {
        public class Request: IRequest<Response>
        {
            public int PageSize { get; set; }
            public int Index { get; set; }
        }

        public class Response: ResponseBase
        {
            public int Length { get; set; }
            public List<PostDto> Entities { get; set; }
        }

        public class Handler: IRequestHandler<Request, Response>
        {
            private readonly IBlogDbContext _context;
        
            public Handler(IBlogDbContext context)
                => _context = context;
        
            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                var query = from post in _context.Posts
                    select post;
                
                var length = await _context.Posts.CountAsync();
                
                var posts = await query.Page(request.Index, request.PageSize)
                    .Select(x => x.ToDto()).ToListAsync();
                
                return new()
                {
                    Length = length,
                    Entities = posts
                };
            }
            
        }
    }
}
