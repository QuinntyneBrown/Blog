using Blog.Api.Models;

namespace Blog.Api.Features
{
    public static class ContentExtensions
    {
        public static ContentDto ToDto(this Content content)
        {
            return new()
            {
                ContentId = content?.ContentId,
                Name = content?.Name,
                Json = content?.Json
            };
        }

    }
}
