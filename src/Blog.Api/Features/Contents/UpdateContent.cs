using Blog.Api.Core;
using Blog.Api.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Blog.Api.Features
{
    public class UpdateContent
    {
        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(request => request.Content).NotNull();
                RuleFor(request => request.Content).SetValidator(new ContentValidator());
            }
        }

        public class Request : IRequest<Response>
        {
            public ContentDto Content { get; set; }
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
                var content = await _context.Contents
                    .SingleAsync(x => x.ContentId == request.Content.ContentId);

                content.Json = request.Content.Json;

                await _context.SaveChangesAsync(cancellationToken);

                return new()
                {
                    Content = content.ToDto()
                };
            }

        }
    }
}
