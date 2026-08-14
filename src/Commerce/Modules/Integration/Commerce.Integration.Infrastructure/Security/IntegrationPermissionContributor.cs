using Commerce.Framework.Contracts.Security;
using Commerce.Integration.Infrastructure.Security;

namespace Commerce.Integration.Infrastructure.Security;

public static class IntegrationPermissions
{
    public const string WebhooksView = "Integration.Webhooks.View";
    public const string WebhooksManage = "Integration.Webhooks.Manage";
    public const string ApiClientsView = "Integration.ApiClients.View";
    public const string ApiClientsManage = "Integration.ApiClients.Manage";
}

public sealed class IntegrationPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Integration";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(IntegrationPermissions.WebhooksView, "View webhook subscriptions and deliveries.", ModuleSystemName),
        new(IntegrationPermissions.WebhooksManage, "Manage webhook subscriptions.", ModuleSystemName),
        new(IntegrationPermissions.ApiClientsView, "View external API clients.", ModuleSystemName),
        new(IntegrationPermissions.ApiClientsManage, "Manage external API clients.", ModuleSystemName)
    ];
}
