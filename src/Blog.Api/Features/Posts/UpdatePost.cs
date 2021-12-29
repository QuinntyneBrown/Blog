using Blog.Api.Core;
using Blog.Api.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Blog.Api.Features
{
    public class UpdatePost
    {
        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(request => request.Post).NotNull();
                RuleFor(request => request.Post).SetValidator(new PostValidator());
            }

        }

        public class Request : IRequest<Response>
        {
            public PostDto Post { get; set; }
        }

        public class Response : ResponseBase
        {
            public PostDto Post { get; set; }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly IBlogDbContext _context;

            public Handler(IBlogDbContext context)
                => _context = context;

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                var post = await _context.Posts.SingleAsync(x => x.PostId == request.Post.PostId);

                post.Body = request.Post.Body;
                post.DatePublished = request.Post.DatePublished;
                post.Published = request.Post.Published;
                post.Slug = request.Post.Slug;
                post.Title = request.Post.Title;
                post.FeaturedImageUrl = request.Post.FeaturedImageUrl;
                post.Abstract = request.Post.Abstract;

                await _context.SaveChangesAsync(cancellationToken);

                return new Response()
                {
                    Post = post.ToDto()
                };
            }

        }
    }
}
