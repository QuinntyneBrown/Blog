using Blog.Api.Core;
using Blog.Api.Interfaces;
using Blog.Api.Models;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Blog.Api.Features
{
    public class CreateDigitalAsset
    {
        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(request => request.DigitalAsset).NotNull();
                RuleFor(request => request.DigitalAsset).SetValidator(new DigitalAssetValidator());
            }
        }

        public class Request : IRequest<Response>
        {
            public DigitalAssetDto DigitalAsset { get; set; }
        }

        public class Response : ResponseBase
        {
            public DigitalAssetDto DigitalAsset { get; set; }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly IBlogDbContext _context;

            public Handler(IBlogDbContext context)
                => _context = context;
            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                var digitalAsset = new DigitalAsset();

                _context.DigitalAssets.Add(digitalAsset);

                await _context.SaveChangesAsync(cancellationToken);

                return new Response()
                {
                    DigitalAsset = digitalAsset.ToDto()
                };
            }

        }
    }
}
