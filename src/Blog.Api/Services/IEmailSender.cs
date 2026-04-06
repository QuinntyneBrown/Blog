namespace Blog.Api.Services;

public interface IEmailSender
{
    Task SendConfirmationEmailAsync(string email, string confirmUrl, CancellationToken cancellationToken = default);
    Task SendNewsletterEmailAsync(string email, string subject, string bodyHtml, string unsubscribeUrl, CancellationToken cancellationToken = default);
}
