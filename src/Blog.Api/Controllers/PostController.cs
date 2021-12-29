using Blog.Api.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace Blog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController
    {
        private readonly IMediator _mediator;

        public PostController(IMediator mediator)
            => _mediator = mediator;

        [HttpGet("{postId}", Name = "GetPostByIdRoute")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(GetPostById.Response), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<GetPostById.Response>> GetById([FromRoute] GetPostById.Request request)
        {
            var response = await _mediator.Send(request);

            if (response.Post == null)
            {
                return new NotFoundObjectResult(request.PostId);
            }

            return response;
        }

        [HttpGet(Name = "GetPostsRoute")]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(GetPosts.Response), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<GetPosts.Response>> Get()
            => await _mediator.Send(new GetPosts.Request());

        [Authorize]
        [HttpPost(Name = "CreatePostRoute")]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(CreatePost.Response), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CreatePost.Response>> Create([FromBody] CreatePost.Request request)
            => await _mediator.Send(request);

        [HttpGet("page/{pageSize}/{index}", Name = "GetPostsPageRoute")]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(GetPostsPage.Response), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<GetPostsPage.Response>> Page([FromRoute] GetPostsPage.Request request)
            => await _mediator.Send(request);

        [Authorize]
        [HttpPut(Name = "UpdatePostRoute")]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(UpdatePost.Response), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<UpdatePost.Response>> Update([FromBody] UpdatePost.Request request)
            => await _mediator.Send(request);

        [Authorize]
        [HttpDelete("{postId}", Name = "RemovePostRoute")]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(RemovePost.Response), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<RemovePost.Response>> Remove([FromRoute] RemovePost.Request request)
            => await _mediator.Send(request);

    }
}
