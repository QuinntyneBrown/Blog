namespace Blog.Api.Common.Models;

public class ApiResponse<T>
{
    public T Data { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data) => new() { Data = data };
}
