namespace Blog.Api.Common.Exceptions;

public class PreconditionFailedException(string message) : Exception(message);
