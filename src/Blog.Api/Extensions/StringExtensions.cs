using Blog.Api.Core;
using static Blog.Api.Core.NamingConvention;

namespace Blog.Api.Extensions
{
    public static class StringExtensions
    {
        public static string Slugify(this string value)
            => new NamingConventionConverter().Convert(Slug, value);
    }
}
