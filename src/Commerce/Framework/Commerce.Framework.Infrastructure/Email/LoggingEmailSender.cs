using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Infrastructure.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Email queued (no-op sender). To={To}, Subject={Subject}, IsHtml={IsHtml}",
            message.To,
            message.Subject,
            message.IsHtml);

        return Task.CompletedTask;
    }
}
