using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FinderController
    {
        private readonly IMediator _mediator;

        public FinderController(IMediator mediator)
        {
            _mediator = mediator;
        }
    }
}
