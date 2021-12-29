using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using Blog.Api.Models;
using Blog.Api.Core;
using Blog.Api.Interfaces;

namespace Blog.Api.Features
{
    public class RemovePost
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
                var post = await _context.Posts.SingleAsync(x => x.PostId == request.PostId);
                
                _context.Posts.Remove(post);
                
                await _context.SaveChangesAsync(cancellationToken);
                
                return new Response()
                {
                    Post = post.ToDto()
                };
            }
            
        }
    }
}
