using System;

namespace Blog.Api.Models
{
    public class Post
    {
        public Guid PostId { get; set; }
        public string Body { get; set; }
        public DateTime? DatePublished { get; set; }
        public bool Published { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string FeaturedImageUrl { get; set; }
        public string Abstract { get; set; }
    }
}
