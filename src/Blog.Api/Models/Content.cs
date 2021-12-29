using Newtonsoft.Json.Linq;
using System;

namespace Blog.Api.Models
{
    public class Content
    {
        public Guid ContentId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public JObject Json { get; set; }
    }
}
