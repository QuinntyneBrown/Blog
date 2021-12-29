using Blog.Api.Helpers;
using Blog.Api.Interfaces;
using Blog.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Blog.Api.Features
{
    public class UploadDigitalAsset
    {
        public class Request : IRequest<Response> { }

        public class Response
        {
            public string Url { get; set; }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly IBlogDbContext _context;
            private readonly IHttpContextAccessor _httpContextAccessor;
            private readonly IConfiguration _configuration;

            public Handler(
                IConfiguration configuration,
                IBlogDbContext context,
                IHttpContextAccessor httpContextAccessor)
            {
                _configuration = configuration;
                _context = context;
                _httpContextAccessor = httpContextAccessor;
            }

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {

                var httpContext = _httpContextAccessor.HttpContext;
                var defaultFormOptions = new FormOptions();
                var digitalAssets = new List<DigitalAsset>();

                if (!MultipartRequestHelper.IsMultipartContentType(httpContext.Request.ContentType))
                    throw new Exception($"Expected a multipart request, but got {httpContext.Request.ContentType}");

                var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(httpContext.Request.ContentType);

                var boundary = MultipartRequestHelper.GetBoundary(
                    mediaTypeHeaderValue,
                    defaultFormOptions.MultipartBoundaryLengthLimit);

                var reader = new MultipartReader(boundary, httpContext.Request.Body);

                var section = await reader.ReadNextSectionAsync();

                while (section != null)
                {

                    var digitalAsset = new DigitalAsset();

                    var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue contentDisposition);

                    if (hasContentDispositionHeader)
                    {
                        if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                        {
                            using (var targetStream = new MemoryStream())
                            {
                                await section.Body.CopyToAsync(targetStream);
                                var name = $"{contentDisposition.FileName}".Trim(new char[] { '"' }).Replace("&", "and");

                                digitalAsset = _context.DigitalAssets.SingleOrDefault(x => x.Name == name);

                                if (digitalAsset == null)
                                {
                                    digitalAsset = new DigitalAsset();
                                    digitalAsset.Name = name;
                                    _context.DigitalAssets.Add(digitalAsset);
                                }

                                digitalAsset.Bytes = StreamHelper.ReadToEnd(targetStream);
                                digitalAsset.ContentType = section.ContentType;
                            }

                            try
                            {
                                using (var image = Image.FromStream(new MemoryStream(digitalAsset.Bytes)))
                                {
                                    digitalAsset.Height = image.PhysicalDimension.Height;
                                    digitalAsset.Width = image.PhysicalDimension.Width;
                                }
                            }
                            catch (Exception e)
                            {

                            }

                        }
                    }

                    digitalAssets.Add(digitalAsset);

                    section = await reader.ReadNextSectionAsync();
                }

                await _context.SaveChangesAsync(cancellationToken);

                return new()
                {
                    Url = $"{_configuration["baseUrl"]}api/digitalassets/serve/{digitalAssets.First().DigitalAssetId}"
                };
            }
        }
    }
}
