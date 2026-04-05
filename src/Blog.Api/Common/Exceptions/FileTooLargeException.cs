namespace Blog.Api.Common.Exceptions;

public class FileTooLargeException(string message) : Exception(message);
