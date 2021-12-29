using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Blog.Api.Core;
using Blog.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features
{
    public class GetContentById
    {
        public class Request: IRequest<Response>
        {
            public Guid ContentId { get; set; }
        }

        public class Response: ResponseBase
        {
            public ContentDto Content { get; set; }
        }

        public class Handler: IRequestHandler<Request, Response>
        {
            private readonly IBlogDbContext _context;
        
            public Handler(IBlogDbContext context)
                => _context = context;
        
            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                return new () {
                    Content = (await _context.Contents.SingleOrDefaultAsync(x => x.ContentId == request.ContentId)).ToDto()
                };
            }
            
        }
    }
}
