namespace Blog.Api.Common.Exceptions;

public class ForbiddenException(string message) : Exception(message);
