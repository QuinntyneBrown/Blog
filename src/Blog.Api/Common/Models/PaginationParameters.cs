namespace Blog.Api.Common.Models;

public class PaginationParameters
{
    private int _pageSize = 9;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : value > 100 ? 100 : value;
    }

    public int Skip => (Page - 1) * PageSize;
}
