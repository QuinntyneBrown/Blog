using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Blog.Api.Core;
using Blog.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Blog.Api.Features
{
    public class GetDigitalAssetById
    {
        public class Request : IRequest<Response>
        {
            public System.Guid DigitalAssetId { get; set; }
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
                return new()
                {
                    DigitalAsset = (await _context.DigitalAssets.SingleOrDefaultAsync(x => x.DigitalAssetId == request.DigitalAssetId)).ToDto()
                };
            }

        }
    }
}
