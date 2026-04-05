namespace Blog.Domain.Exceptions;

public class RateLimitExceededException(string message) : Exception(message);
