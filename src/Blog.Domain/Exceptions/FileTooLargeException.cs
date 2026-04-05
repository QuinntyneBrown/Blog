namespace Blog.Domain.Exceptions;

public class FileTooLargeException(string message) : Exception(message);
