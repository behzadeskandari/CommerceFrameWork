using System.Text.Json;
using Commerce.Cms.Application.Abstractions;
using Commerce.Cms.Application.Security;
using Commerce.Cms.Contracts.Admin;
using Commerce.Cms.Domain.Entities;
using Commerce.Cms.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Cms.Application.Admin;

public sealed class WidgetAdminService(ICmsRepository repository, IContentHtmlSanitizer sanitizer) : IWidgetAdminService
{
    public async Task<Result<IReadOnlyList<WidgetZoneDto>>> ListZonesAsync(CancellationToken cancellationToken = default)
    {
        await repository.EnsureWidgetZonesSeededAsync(cancellationToken).ConfigureAwait(false);
        var zones = await repository.ListWidgetZonesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<WidgetZoneDto>>(zones.Select(z => new WidgetZoneDto(z.Id, z.SystemName, z.Name, z.Description, z.DisplayOrder)).ToList());
    }

    public async Task<Result<IReadOnlyList<WidgetInstanceDto>>> ListInstancesAsync(int? storeId, string? zoneSystemName, CancellationToken cancellationToken = default)
    {
        if (!storeId.HasValue)
        {
            return Result.Success<IReadOnlyList<WidgetInstanceDto>>([]);
        }

        int? zoneId = null;
        if (!string.IsNullOrWhiteSpace(zoneSystemName))
        {
            var zone = await repository.GetWidgetZoneBySystemNameAsync(zoneSystemName, cancellationToken).ConfigureAwait(false);
            zoneId = zone?.Id;
        }

        var instances = await repository.ListWidgetInstancesAsync(storeId.Value, zoneId, cancellationToken).ConfigureAwait(false);
        var zones = (await repository.ListWidgetZonesAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(x => x.Id);
        return Result.Success<IReadOnlyList<WidgetInstanceDto>>(instances.Select(i => MapInstance(i, zones)).ToList());
    }

    public async Task<Result<WidgetInstanceDto>> CreateInstanceAsync(CreateWidgetInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateWidgetConfig(request.WidgetType, request.ConfigurationJson);
        if (validation is not null)
        {
            return Result.Failure<WidgetInstanceDto>(Error.Validation(validation));
        }

        var sanitizedConfig = SanitizeWidgetConfig(request.WidgetType, request.ConfigurationJson);
        var instance = WidgetInstance.Create(
            request.StoreId,
            request.WidgetZoneId,
            request.WidgetType,
            sanitizedConfig,
            request.LanguageId,
            request.DisplayOrder,
            request.IsActive);
        await repository.AddWidgetInstanceAsync(instance, cancellationToken).ConfigureAwait(false);
        var zone = await repository.GetWidgetZoneByIdAsync(instance.WidgetZoneId, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapInstance(instance, zone is null ? [] : new Dictionary<int, WidgetZone> { [zone.Id] = zone }));
    }

    public async Task<Result<WidgetInstanceDto>> UpdateInstanceAsync(int id, UpdateWidgetInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var instance = await repository.GetWidgetInstanceByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (instance is null)
        {
            return Result.Failure<WidgetInstanceDto>(Error.NotFound($"Widget instance '{id}' was not found."));
        }

        var validation = ValidateWidgetConfig(request.WidgetType, request.ConfigurationJson);
        if (validation is not null)
        {
            return Result.Failure<WidgetInstanceDto>(Error.Validation(validation));
        }

        instance.Update(request.WidgetType, SanitizeWidgetConfig(request.WidgetType, request.ConfigurationJson), request.LanguageId, request.DisplayOrder, request.IsActive);
        await repository.SaveWidgetInstanceAsync(instance, cancellationToken).ConfigureAwait(false);
        var zone = await repository.GetWidgetZoneByIdAsync(instance.WidgetZoneId, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapInstance(instance, zone is null ? [] : new Dictionary<int, WidgetZone> { [zone.Id] = zone }));
    }

    public async Task<Result> DeleteInstanceAsync(int id, CancellationToken cancellationToken = default)
    {
        var instance = await repository.GetWidgetInstanceByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (instance is null)
        {
            return Result.Failure(Error.NotFound($"Widget instance '{id}' was not found."));
        }

        await repository.DeleteWidgetInstanceAsync(instance, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private string SanitizeWidgetConfig(WidgetType type, string configurationJson)
    {
        if (type != WidgetType.HtmlBlock)
        {
            return configurationJson;
        }

        try
        {
            using var doc = JsonDocument.Parse(configurationJson);
            if (!doc.RootElement.TryGetProperty("html", out var htmlElement))
            {
                return configurationJson;
            }

            var html = sanitizer.Sanitize(htmlElement.GetString());
            return JsonSerializer.Serialize(new { html });
        }
        catch (JsonException)
        {
            return "{}";
        }
    }

    private static string? ValidateWidgetConfig(WidgetType type, string configurationJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(configurationJson);
            return type switch
            {
                WidgetType.HtmlBlock when !doc.RootElement.TryGetProperty("html", out _) => "HtmlBlock widget requires 'html' property.",
                WidgetType.TopicEmbed when !doc.RootElement.TryGetProperty("systemName", out _) => "TopicEmbed widget requires 'systemName' property.",
                WidgetType.MenuEmbed when !doc.RootElement.TryGetProperty("systemName", out _) => "MenuEmbed widget requires 'systemName' property.",
                _ => null
            };
        }
        catch (JsonException)
        {
            return "Configuration must be valid JSON.";
        }
    }

    private static WidgetInstanceDto MapInstance(WidgetInstance instance, IReadOnlyDictionary<int, WidgetZone> zones) =>
        new(
            instance.Id,
            instance.StoreId,
            instance.WidgetZoneId,
            zones.TryGetValue(instance.WidgetZoneId, out var zone) ? zone.SystemName : string.Empty,
            instance.WidgetType,
            instance.ConfigurationJson,
            instance.LanguageId,
            instance.DisplayOrder,
            instance.IsActive);
}
