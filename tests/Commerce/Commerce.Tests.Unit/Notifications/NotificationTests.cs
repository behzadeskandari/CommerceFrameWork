using Commerce.Notifications.Contracts.Dispatch;
using Commerce.Notifications.Application.Abstractions;
using Commerce.Notifications.Application.Dispatch;
using Commerce.Notifications.Application.Templates;
using Commerce.Notifications.Contracts.Dispatch;
using Commerce.Notifications.Domain.Entities;
using Commerce.Notifications.Domain.Enums;
using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Scheduling;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Commerce.Tests.Unit.Notifications;

public sealed class NotificationTemplateRendererTests
{
    [Fact]
    public void Render_ReplacesKnownVariables()
    {
        var result = NotificationTemplateRenderer.Render(
            "Hello {{firstName}}, order {{orderNumber}}",
            new Dictionary<string, string>
            {
                ["firstName"] = "Ada",
                ["orderNumber"] = "1001"
            });

        Assert.Equal("Hello Ada, order 1001", result);
    }

    [Fact]
    public void Render_LeavesUnknownTokensUntouched()
    {
        var result = NotificationTemplateRenderer.Render(
            "Hello {{firstName}}, {{unknown}}",
            new Dictionary<string, string> { ["firstName"] = "Ada" });

        Assert.Equal("Hello Ada, {{unknown}}", result);
    }
}

public sealed class NotificationTemplateSelectorTests
{
    [Fact]
    public void Select_PrefersStoreSpecificTemplate()
    {
        var global = Template("global", storeId: null, languageId: null);
        var storeSpecific = Template("store", storeId: 1, languageId: null);

        var selected = NotificationTemplateSelector.Select([global, storeSpecific], storeId: 1, languageId: null);

        Assert.Single(selected);
        Assert.Equal("store", selected[0].SystemName);
    }

    [Fact]
    public void Select_ExcludesWrongStoreTemplate()
    {
        var otherStore = Template("other", storeId: 2, languageId: null);
        var global = Template("global", storeId: null, languageId: null);

        var selected = NotificationTemplateSelector.Select([otherStore, global], storeId: 1, languageId: null);

        Assert.Single(selected);
        Assert.Equal("global", selected[0].SystemName);
    }

    [Fact]
    public void Select_PrefersLanguageSpecificTemplate()
    {
        var languageNeutral = Template("neutral", storeId: null, languageId: null);
        var languageSpecific = Template("fa", storeId: null, languageId: 2);

        var selected = NotificationTemplateSelector.Select([languageNeutral, languageSpecific], storeId: null, languageId: 2);

        Assert.Single(selected);
        Assert.Equal("fa", selected[0].SystemName);
    }

    [Fact]
    public void Select_ReturnsOneTemplatePerChannel()
    {
        var email = Template("email", storeId: null, languageId: null, channel: NotificationChannel.Email);
        var sms = Template("sms", storeId: null, languageId: null, channel: NotificationChannel.Sms);

        var selected = NotificationTemplateSelector.Select([email, sms], storeId: null, languageId: null);

        Assert.Equal(2, selected.Count);
    }

    private static NotificationTemplate Template(
        string systemName,
        int? storeId,
        int? languageId,
        NotificationChannel channel = NotificationChannel.Email) =>
        NotificationTemplate.Create(
            systemName,
            NotificationEventType.OrderCreated,
            channel,
            "Subject",
            "Body",
            languageId,
            storeId,
            null,
            isEnabled: true);
}

public sealed class NotificationDispatcherTests
{
    [Fact]
    public async Task DispatchEventAsync_SendsThroughMatchingProvider()
    {
        var repository = new FakeNotificationsRepository();
        var emailProvider = new RecordingProvider(NotificationChannel.Email);
        var dispatcher = CreateDispatcher(repository, [emailProvider]);

        repository.Templates.Add(Template("order-email", NotificationChannel.Email));

        await dispatcher.DispatchEventAsync(
            new NotificationEventRequest(
                NotificationEventType.OrderCreated,
                StoreId: 1,
                CustomerId: 10,
                LanguageId: null,
                RecipientEmail: "customer@example.com",
                RecipientPhone: null,
                new Dictionary<string, string> { ["orderNumber"] = "1001" }),
            CancellationToken.None);

        Assert.Single(emailProvider.Requests);
        Assert.Equal("customer@example.com", emailProvider.Requests[0].Recipient);
        Assert.Single(repository.Logs);
        Assert.Equal(NotificationDeliveryStatus.Sent, repository.Logs[0].Status);
    }

    [Fact]
    public async Task DispatchEventAsync_MarksFailedWhenProviderFails()
    {
        var repository = new FakeNotificationsRepository();
        var failingProvider = new RecordingProvider(NotificationChannel.Email, success: false, error: "SMTP down");
        var dispatcher = CreateDispatcher(repository, [failingProvider]);

        repository.Templates.Add(Template("order-email", NotificationChannel.Email));

        await dispatcher.DispatchEventAsync(
            new NotificationEventRequest(
                NotificationEventType.OrderCreated,
                StoreId: null,
                CustomerId: null,
                LanguageId: null,
                RecipientEmail: "customer@example.com",
                RecipientPhone: null,
                new Dictionary<string, string>()),
            CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.Pending, repository.Logs[0].Status);
        Assert.Equal(1, repository.Logs[0].AttemptCount);
        Assert.NotNull(repository.Logs[0].NextRetryAtUtc);
        Assert.Equal("SMTP down", repository.Logs[0].LastError);
    }

    [Fact]
    public async Task RetryLogAsync_UsesStoredBodyForRedelivery()
    {
        var repository = new FakeNotificationsRepository();
        var provider = new RecordingProvider(NotificationChannel.Email);
        var dispatcher = CreateDispatcher(repository, [provider]);

        var log = NotificationLog.CreatePending(
            templateId: 1,
            NotificationEventType.OrderCreated,
            NotificationChannel.Email,
            storeId: null,
            customerId: null,
            recipient: "customer@example.com",
            subject: "Order 1001",
            body: "Thanks for your order");

        log.MarkFailed("temporary", DateTime.UtcNow.AddMinutes(2), incrementAttempt: false);
        repository.Logs.Add(log);

        await dispatcher.RetryLogAsync(log, CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.Sent, log.Status);
        Assert.Equal("Thanks for your order", provider.Requests[^1].Body);
    }

    private static NotificationDispatcher CreateDispatcher(
        INotificationsRepository repository,
        IReadOnlyList<INotificationChannelProvider> providers) =>
        new(
            repository,
            providers,
            new NoOpBackgroundJobScheduler(),
            new NoOpCorrelationContext(),
            NullLogger<NotificationDispatcher>.Instance);

    private sealed class NoOpCorrelationContext : ICorrelationContext
    {
        public string? CorrelationId => null;
        public string? RequestId => null;
        public string? TraceId => null;
    }

    private sealed class NoOpBackgroundJobScheduler : IBackgroundJobScheduler
    {
        public Task<Result<int>> EnqueueAsync(EnqueueBackgroundJobRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(0));

        public Task<Result<int>> ScheduleAsync(ScheduleBackgroundJobRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(0));

        public Task<Result<int>> EnqueueDelayedAsync(
            string jobType,
            TimeSpan delay,
            string? payloadJson = null,
            int priority = 0,
            int maxAttempts = 3,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(0));

        public Task<Result> RegisterRecurringAsync(RegisterRecurringJobRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());

        public Task<Result> CancelAsync(int jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private static NotificationTemplate Template(string systemName, NotificationChannel channel) =>
        NotificationTemplate.Create(
            systemName,
            NotificationEventType.OrderCreated,
            channel,
            "Order {{orderNumber}}",
            "Body {{orderNumber}}",
            languageId: null,
            storeId: null,
            variablesJson: null,
            isEnabled: true);

    private sealed class FakeNotificationsRepository : INotificationsRepository
    {
        public List<NotificationTemplate> Templates { get; } = [];
        public List<NotificationLog> Logs { get; } = [];

        public Task<IReadOnlyList<NotificationTemplate>> GetEnabledTemplatesForEventAsync(
            NotificationEventType eventType,
            int? storeId,
            int? languageId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationTemplate>>(Templates.Where(x => x.IsEnabled).ToList());

        public Task AddLogAsync(NotificationLog log, CancellationToken cancellationToken = default)
        {
            Logs.Add(log);
            return Task.CompletedTask;
        }

        public Task SaveLogAsync(NotificationLog log, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<NotificationTemplate>> ListTemplatesAsync(int? storeId, NotificationEventType? eventType, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<NotificationTemplate?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<NotificationTemplate?> GetTemplateBySystemNameAsync(string systemName, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task SaveTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task DeleteTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<NotificationLog?> GetLogByIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<NotificationLog>> ListLogsAsync(int? storeId, NotificationDeliveryStatus? status, int? customerId, int take, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<NotificationLog>> GetRetryCandidatesAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddInAppNotificationAsync(InAppNotification notification, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<InAppNotification>> ListUnreadInAppAsync(int customerId, int? storeId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<InAppNotification?> GetInAppByIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task SaveInAppNotificationAsync(InAppNotification notification, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class RecordingProvider(NotificationChannel channel, bool success = true, string? error = null)
        : INotificationChannelProvider
    {
        public List<NotificationDeliveryRequest> Requests { get; } = [];

        public NotificationChannel Channel => channel;

        public Task<NotificationDeliveryResult> SendAsync(NotificationDeliveryRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(new NotificationDeliveryResult(success, error));
        }
    }
}

public sealed class NotificationAuthorizationTests
{
    [Fact]
    public void NotificationPermissions_AreDefined()
    {
        Assert.Equal("Notifications.View", Commerce.Notifications.Infrastructure.Security.NotificationPermissions.View);
        Assert.Equal("Notifications.Manage", Commerce.Notifications.Infrastructure.Security.NotificationPermissions.Manage);
    }
}
