namespace Blog.Api.Common.Exceptions;

public class RateLimitExceededException(string message) : Exception(message);
