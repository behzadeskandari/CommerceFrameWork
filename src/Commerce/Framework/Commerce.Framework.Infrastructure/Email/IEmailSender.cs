namespace Commerce.Framework.Infrastructure.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string Body,
    bool IsHtml = false,
    string? From = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
