using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Infrastructure.Sms;

public sealed class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SMS to {Recipient}: {Body}", MaskRecipient(message.To), message.Body);
        return Task.CompletedTask;
    }

    private static string MaskRecipient(string recipient) =>
        recipient.Length <= 4 ? "****" : $"{recipient[..2]}***{recipient[^2..]}";
}
