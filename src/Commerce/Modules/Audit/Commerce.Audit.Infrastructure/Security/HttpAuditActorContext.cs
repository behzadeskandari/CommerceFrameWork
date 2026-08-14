using System.Security.Claims;
using Commerce.Framework.Contracts.Audit;
using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Contracts.Security;
using Microsoft.AspNetCore.Http;

namespace Commerce.Audit.Infrastructure.Security;

public interface IAuditActorContext
{
    AuditPublishRequest Enrich(AuditPublishRequest request);
}

public sealed class HttpAuditActorContext(
    IHttpContextAccessor httpContextAccessor,
    ICorrelationContext correlationContext) : IAuditActorContext
{
    public AuditPublishRequest Enrich(AuditPublishRequest request)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return request;
        }

        var user = httpContext.User;
        var actorType = ResolveActorType(user);
        var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorDisplay = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        var correlationId = correlationContext.CorrelationId
            ?? httpContext.TraceIdentifier;

        return request with
        {
            ActorType = request.ActorType == AuditActorType.System ? actorType : request.ActorType,
            ActorId = request.ActorId ?? actorId,
            ActorDisplay = request.ActorDisplay ?? actorDisplay,
            IpAddress = request.IpAddress ?? ipAddress,
            UserAgent = request.UserAgent ?? userAgent,
            CorrelationId = request.CorrelationId ?? correlationId
        };
    }

    private static AuditActorType ResolveActorType(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return AuditActorType.Anonymous;
        }

        if (user.IsInRole(CommerceRoles.Administrator))
        {
            return AuditActorType.Administrator;
        }

        if (user.IsInRole(CommerceRoles.Customer))
        {
            return AuditActorType.Customer;
        }

        return AuditActorType.Administrator;
    }
}

public static class AuditActorContextExtensions
{
    public static AuditPublishRequest WithActor(this AuditPublishRequest request, IAuditActorContext actorContext) =>
        actorContext.Enrich(request);
}
