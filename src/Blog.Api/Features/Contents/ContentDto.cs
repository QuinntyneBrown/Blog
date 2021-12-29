using Blog.Api.Extensions;
using Newtonsoft.Json.Linq;
using System;

namespace Blog.Api.Features
{
    public class ContentDto
    {
        public Guid? ContentId { get; set; }
        public string Name { get; set; }
        public string Slug => Name.Slugify();
        public JObject Json { get; set; }
    }
}
