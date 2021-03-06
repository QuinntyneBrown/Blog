using Blog.Api.Core;

namespace Blog.Api.Interfaces
{
    public interface INamingConventionConverter
    {
        string Convert(NamingConvention from, NamingConvention to, string value);

        string Convert(NamingConvention to, string value);

        string Convert(NamingConvention to, string value, bool pluralize);
    }
}