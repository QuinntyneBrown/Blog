namespace Blog.Api.Common.Exceptions;

public class TooManyRequestsException(string message) : Exception(message);
