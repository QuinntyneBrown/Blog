using Blog.Api.Core;
using Blog.Api.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Blog.Api.Features
{
    public class RemoveContent
    {
        public class Request : IRequest<Response>
        {
            public Guid ContentId { get; set; }
        }

        public class Response : ResponseBase
        {
            public ContentDto Content { get; set; }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly IBlogDbContext _context;

            public Handler(IBlogDbContext context)
                => _context = context;

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                var content = await _context.Contents.SingleAsync(x => x.ContentId == request.ContentId);

                _context.Contents.Remove(content);

                await _context.SaveChangesAsync(cancellationToken);

                return new()
                {
                    Content = content.ToDto()
                };
            }

        }
    }
}
