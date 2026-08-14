namespace Commerce.Framework.Infrastructure.Sms;

public sealed record SmsMessage(string To, string Body);

public interface ISmsSender
{
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}
