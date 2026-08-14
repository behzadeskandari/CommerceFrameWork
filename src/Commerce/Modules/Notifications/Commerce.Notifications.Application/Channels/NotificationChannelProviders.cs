using Commerce.Framework.Infrastructure.Email;
using Commerce.Notifications.Application.Abstractions;
using Commerce.Notifications.Contracts.Dispatch;
using Commerce.Notifications.Domain.Enums;

namespace Commerce.Notifications.Application.Channels;

public sealed class EmailNotificationChannelProvider(IEmailSender emailSender) : INotificationChannelProvider
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await emailSender.SendAsync(
                new EmailMessage(request.Recipient, request.Subject, request.Body, request.IsHtml),
                cancellationToken).ConfigureAwait(false);
            return new NotificationDeliveryResult(true);
        }
        catch (Exception ex)
        {
            return new NotificationDeliveryResult(false, ex.Message);
        }
    }
}

public sealed class SmsNotificationChannelProvider(Commerce.Framework.Infrastructure.Sms.ISmsSender smsSender) : INotificationChannelProvider
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await smsSender.SendAsync(
                new Commerce.Framework.Infrastructure.Sms.SmsMessage(request.Recipient, request.Body),
                cancellationToken).ConfigureAwait(false);
            return new NotificationDeliveryResult(true);
        }
        catch (Exception ex)
        {
            return new NotificationDeliveryResult(false, ex.Message);
        }
    }
}

public sealed class InAppNotificationChannelProvider(INotificationsRepository repository) : INotificationChannelProvider
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(request.Recipient, out var customerId))
        {
            return new NotificationDeliveryResult(false, "In-app notifications require a customer id recipient.");
        }

        try
        {
            var notification = Domain.Entities.InAppNotification.Create(
                customerId,
                storeId: null,
                request.Subject,
                request.Body);

            await repository.AddInAppNotificationAsync(notification, cancellationToken).ConfigureAwait(false);
            return new NotificationDeliveryResult(true);
        }
        catch (Exception ex)
        {
            return new NotificationDeliveryResult(false, ex.Message);
        }
    }
}
