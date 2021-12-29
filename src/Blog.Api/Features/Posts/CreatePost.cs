using Blog.Api.Core;
using Blog.Api.Interfaces;
using Blog.Api.Models;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Blog.Api.Features
{
    public class CreatePost
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
                var post = new Post()
                {
                    Body = request.Post.Body,
                    DatePublished = request.Post.DatePublished,
                    Published = request.Post.Published,
                    Slug = request.Post.Slug,
                    Title = request.Post.Title,
                    FeaturedImageUrl = request.Post.FeaturedImageUrl,
                    Abstract = request.Post.Abstract
                };

                _context.Posts.Add(post);

                await _context.SaveChangesAsync(cancellationToken);

                return new()
                {
                    Post = post.ToDto()
                };
            }

        }
    }
}
