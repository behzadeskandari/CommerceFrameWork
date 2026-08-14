using Commerce.Framework.Contracts.Audit;
using Commerce.Framework.Contracts.Security;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;

namespace Commerce.Tests.Unit.Audit;

public sealed class Phase37AuthorizationBypassTests
{
    [Fact]
    public async Task AccessDenied_IsAudited_WhenPermissionMissing()
    {
        var publisher = new RecordingAuditPublisher();
        var coordinator = new AuthorizationAuditCoordinator(new DenyAllPermissionService(), publisher);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/admin/orders";
        httpContext.Request.Method = "GET";

        var granted = await coordinator.TryAuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            "Orders.View",
            httpContext);

        Assert.False(granted);
        var request = Assert.Single(publisher.Requests);
        Assert.Equal(AuditCategory.Authorization, request.Category);
        Assert.Equal(AuditActions.AccessDenied, request.Action);
        Assert.False(request.Success);
        Assert.Equal("Orders.View", request.EntityId);
    }

    [Fact]
    public async Task AccessGranted_DoesNotAudit_WhenPermissionPresent()
    {
        var publisher = new RecordingAuditPublisher();
        var coordinator = new AuthorizationAuditCoordinator(new AllowAllPermissionService(), publisher);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/admin/orders";

        var granted = await coordinator.TryAuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            "Orders.View",
            httpContext);

        Assert.True(granted);
        Assert.Empty(publisher.Requests);
    }

    /// <summary>
    /// Mirrors host authorization audit behavior for bypass detection tests.
    /// </summary>
    private sealed class AuthorizationAuditCoordinator(
        IPermissionService permissionService,
        IAuditPublisher auditPublisher)
    {
        public async Task<bool> TryAuthorizeAsync(
            ClaimsPrincipal user,
            string permission,
            HttpContext httpContext)
        {
            if (await permissionService.HasPermissionAsync(user, permission, httpContext.RequestAborted).ConfigureAwait(false))
            {
                return true;
            }

            await auditPublisher.PublishAsync(new AuditPublishRequest(
                AuditCategory.Authorization,
                AuditActions.AccessDenied,
                Success: false,
                EntityType: "Permission",
                EntityId: permission,
                Details: new Dictionary<string, string?>
                {
                    ["path"] = httpContext.Request.Path.Value,
                    ["method"] = httpContext.Request.Method
                }), httpContext.RequestAborted).ConfigureAwait(false);

            return false;
        }
    }

    private sealed class RecordingAuditPublisher : IAuditPublisher
    {
        public List<AuditPublishRequest> Requests { get; } = [];

        public Task PublishAsync(AuditPublishRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class DenyAllPermissionService : IPermissionService
    {
        public IReadOnlyList<PermissionDefinition> GetAllPermissions() => Array.Empty<PermissionDefinition>();

        public Task<IReadOnlyList<string>> GetPermissionsForUserAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string permission, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class AllowAllPermissionService : IPermissionService
    {
        public IReadOnlyList<PermissionDefinition> GetAllPermissions() => Array.Empty<PermissionDefinition>();

        public Task<IReadOnlyList<string>> GetPermissionsForUserAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(["Orders.View"]);

        public Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string permission, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
