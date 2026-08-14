using Commerce.Audit.Application.Writing;
using Commerce.Audit.Contracts;
using Commerce.Audit.Domain.Entities;
using Commerce.Framework.Contracts.Audit;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Commerce.Tests.Unit.Audit;

public sealed class Phase37AuditChainTests
{
    [Fact]
    public async Task VerifyChainAsync_ReturnsValid_ForUnmodifiedEntries()
    {
        await using var provider = AuditTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<AuditWriter>();
        var queryService = scope.ServiceProvider.GetRequiredService<IAuditQueryService>();

        await writer.PublishAsync(new AuditPublishRequest(
            AuditCategory.Admin,
            AuditActions.AdminRequest,
            Success: true,
            EntityType: "HttpRequest",
            EntityId: "/api/admin/products"), CancellationToken.None);

        await writer.PublishAsync(new AuditPublishRequest(
            AuditCategory.Order,
            AuditActions.OrderCancelled,
            Success: true,
            EntityType: "Order",
            EntityId: "42"), CancellationToken.None);

        var result = await queryService.VerifyChainAsync(cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsValid);
        Assert.Equal(2, result.Value.VerifiedCount);
    }

    [Fact]
    public async Task VerifyChainAsync_DetectsTamperedEntryHash()
    {
        await using var provider = AuditTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<AuditWriter>();
        var queryService = scope.ServiceProvider.GetRequiredService<IAuditQueryService>();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();

        await writer.PublishAsync(new AuditPublishRequest(
            AuditCategory.Security,
            AuditActions.LoginSucceeded,
            Success: true,
            EntityType: "User",
            EntityId: "1"), CancellationToken.None);

        var entry = await db.Set<AuditEntry>().SingleAsync();
        db.Entry(entry).Property(nameof(AuditEntry.EntryHash)).CurrentValue = "tampered";
        await db.SaveChangesAsync();

        var result = await queryService.VerifyChainAsync(cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsValid);
        Assert.Equal(entry.Id, result.Value.FirstInvalidEntryId);
    }

    [Fact]
    public async Task ApplyRetentionPolicyAsync_DeletesEntriesOlderThanCutoff()
    {
        await using var provider = AuditTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var queryService = scope.ServiceProvider.GetRequiredService<IAuditQueryService>();

        db.Set<AuditEntry>().Add(AuditEntry.Create(
            null,
            DateTime.UtcNow.AddDays(-400),
            Commerce.Audit.Domain.Enums.AuditCategory.Admin,
            AuditActions.AdminRequest,
            Commerce.Audit.Domain.Enums.AuditActorType.Administrator,
            null,
            null,
            "HttpRequest",
            "/old",
            null,
            null,
            null,
            true,
            null,
            AuditEntry.GenesisHash,
            "legacyhash"));
        await db.SaveChangesAsync();

        var result = await queryService.ApplyRetentionPolicyAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.DeletedCount);
        Assert.Empty(await db.Set<AuditEntry>().ToListAsync());
    }
}
