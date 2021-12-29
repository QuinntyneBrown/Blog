using Blog.Api.Models;

namespace Blog.Api.Features
{
    public static class PostExtensions
    {
        public static PostDto ToDto(this Post post)
        {
            return new()
            {
                PostId = post.PostId,
                Body = post.Body,
                DatePublished = post.DatePublished,
                Published = post.Published,
                Title = post.Title,
                FeaturedImageUrl = post.FeaturedImageUrl,
                Abstract = post.Abstract
            };
        }

    }
}
