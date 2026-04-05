namespace Blog.Api.Services;

public interface ISlugGenerator
{
    string Generate(string title);
}
