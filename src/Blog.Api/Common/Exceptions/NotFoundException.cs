namespace Blog.Api.Common.Exceptions;

public class NotFoundException(string message) : Exception(message);
