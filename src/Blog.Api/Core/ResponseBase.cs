using System.Collections.Generic;

namespace Blog.Api.Core
{
    public class ResponseBase
    {
        public List<string> ValidationErrors { get; set; }
    }
}
