using Blog.Api.Extensions;
using System;

namespace Blog.Api.Features
{
    public class PostDto
    {
        public Guid? PostId { get; set; }
        public string Body { get; set; }
        public DateTime? DatePublished { get; set; }
        public bool Published { get; set; }
        public string Slug => Title.Slugify();
        public string Title { get; set; }
        public string FeaturedImageUrl { get; set; }
        public string Abstract { get; set; }
    }
}
