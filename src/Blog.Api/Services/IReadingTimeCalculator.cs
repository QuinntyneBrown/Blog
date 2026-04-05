namespace Blog.Api.Services;

public interface IReadingTimeCalculator
{
    int Calculate(string markdownBody);
}
