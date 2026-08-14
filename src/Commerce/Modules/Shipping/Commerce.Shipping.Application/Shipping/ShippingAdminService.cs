using Commerce.Framework.Contracts.Configuration;
using Commerce.Shipping.Application.Abstractions;
using Commerce.Shipping.Contracts.Admin;
using Commerce.Shipping.Contracts.Shipments;
using Commerce.Shipping.Contracts.Shipping;
using Commerce.Shipping.Domain.Entities;
using Commerce.Shipping.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Shipping.Application.Shipping;

public sealed class ShippingAdminService(
    IShippingRepository repository,
    ShippingSettings shippingSettings,
    ISettingService settingService) : IShippingAdminService
{
    public async Task<Result<IReadOnlyList<ShippingMethodSummaryDto>>> ListMethodsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var methods = await repository.ListMethodsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<ShippingMethodSummaryDto>>(
            methods.Where(m => !m.IsDeleted).Select(MapMethodSummary).ToList());
    }

    public async Task<Result<ShippingMethodDetailDto>> GetMethodAsync(int id, CancellationToken cancellationToken = default)
    {
        var method = await repository.GetMethodByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return method is null || method.IsDeleted
            ? Result.Failure<ShippingMethodDetailDto>(Error.NotFound($"Shipping method '{id}' was not found."))
            : Result.Success(MapMethodDetail(method));
    }

    public async Task<Result<ShippingMethodDetailDto>> CreateMethodAsync(CreateShippingMethodRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var method = ShippingMethod.Create(
            request.StoreId,
            request.Name,
            request.SystemName,
            request.Description,
            request.ProviderSystemName,
            request.IsActive,
            request.DisplayOrder,
            request.RequiresAddress,
            request.SupportsTracking,
            request.EstimatedDeliveryDaysMin,
            request.EstimatedDeliveryDaysMax);

        await repository.AddMethodAsync(method, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapMethodDetail(method));
    }

    public async Task<Result<ShippingMethodDetailDto>> UpdateMethodAsync(int id, UpdateShippingMethodRequest request, CancellationToken cancellationToken = default)
    {
        var method = await repository.GetMethodByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (method is null || method.IsDeleted)
        {
            return Result.Failure<ShippingMethodDetailDto>(Error.NotFound($"Shipping method '{id}' was not found."));
        }

        method.Update(
            request.Name,
            request.Description,
            request.IsActive,
            request.DisplayOrder,
            request.RequiresAddress,
            request.SupportsTracking,
            request.EstimatedDeliveryDaysMin,
            request.EstimatedDeliveryDaysMax);

        await repository.SaveMethodAsync(method, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapMethodDetail(method));
    }

    public async Task<Result> DeleteMethodAsync(int id, CancellationToken cancellationToken = default)
    {
        var method = await repository.GetMethodByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (method is null || method.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Shipping method '{id}' was not found."));
        }

        method.SoftDelete();
        await repository.SaveMethodAsync(method, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<ShippingZoneSummaryDto>>> ListZonesAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var zones = await repository.ListZonesAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<ShippingZoneSummaryDto>>(
            zones.Where(z => !z.IsDeleted).Select(MapZoneSummary).ToList());
    }

    public async Task<Result<ShippingZoneDetailDto>> GetZoneAsync(int id, CancellationToken cancellationToken = default)
    {
        var zone = await repository.GetZoneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return zone is null || zone.IsDeleted
            ? Result.Failure<ShippingZoneDetailDto>(Error.NotFound($"Shipping zone '{id}' was not found."))
            : Result.Success(MapZoneDetail(zone));
    }

    public async Task<Result<ShippingZoneDetailDto>> CreateZoneAsync(CreateShippingZoneRequest request, CancellationToken cancellationToken = default)
    {
        var zone = ShippingZone.Create(
            request.StoreId,
            request.Name,
            request.SystemName,
            request.IsDefault,
            request.IsActive,
            request.DisplayOrder);

        ApplyZoneRules(zone, request.Countries, request.States, request.PostalRules);
        await repository.AddZoneAsync(zone, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapZoneDetail(zone));
    }

    public async Task<Result<ShippingZoneDetailDto>> UpdateZoneAsync(int id, UpdateShippingZoneRequest request, CancellationToken cancellationToken = default)
    {
        var zone = await repository.GetZoneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (zone is null || zone.IsDeleted)
        {
            return Result.Failure<ShippingZoneDetailDto>(Error.NotFound($"Shipping zone '{id}' was not found."));
        }

        zone.Update(request.Name, request.IsDefault, request.IsActive, request.DisplayOrder);
        ApplyZoneRules(zone, request.Countries, request.States, request.PostalRules);
        await repository.SaveZoneAsync(zone, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapZoneDetail(zone));
    }

    public async Task<Result> DeleteZoneAsync(int id, CancellationToken cancellationToken = default)
    {
        var zone = await repository.GetZoneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (zone is null || zone.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Shipping zone '{id}' was not found."));
        }

        zone.SoftDelete();
        await repository.SaveZoneAsync(zone, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<ShippingRateSummaryDto>>> ListRatesAsync(int? storeId, int? methodId, CancellationToken cancellationToken = default)
    {
        var rates = await repository.ListRatesAsync(storeId, methodId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<ShippingRateSummaryDto>>(
            rates.Where(r => !r.IsDeleted).Select(MapRateSummary).ToList());
    }

    public async Task<Result<ShippingRateDetailDto>> GetRateAsync(int id, CancellationToken cancellationToken = default)
    {
        var rate = await repository.GetRateByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return rate is null || rate.IsDeleted
            ? Result.Failure<ShippingRateDetailDto>(Error.NotFound($"Shipping rate '{id}' was not found."))
            : Result.Success(MapRateDetail(rate));
    }

    public async Task<Result<ShippingRateDetailDto>> CreateRateAsync(CreateShippingRateRequest request, CancellationToken cancellationToken = default)
    {
        var rate = request.RateType switch
        {
            ShippingRateType.Flat => ShippingRate.CreateFlat(
                request.StoreId,
                request.ShippingMethodId,
                request.ShippingZoneId,
                request.CurrencyCode,
                request.BasePrice,
                request.FreeShippingThreshold,
                request.MinOrderSubtotal,
                request.MaxOrderSubtotal,
                request.PricePerWeightUnit),
            ShippingRateType.WeightBased => ShippingRate.CreateWeightBased(
                request.StoreId,
                request.ShippingMethodId,
                request.ShippingZoneId,
                request.CurrencyCode,
                request.BasePrice,
                request.PricePerWeightUnit ?? 0m,
                request.MinWeightGrams,
                request.MaxWeightGrams,
                request.FreeShippingThreshold),
            ShippingRateType.OrderSubtotalBased => ShippingRate.CreateOrderSubtotalBased(
                request.StoreId,
                request.ShippingMethodId,
                request.ShippingZoneId,
                request.CurrencyCode,
                request.BasePrice,
                request.OrderSubtotalPercentage ?? 0m,
                request.MinOrderSubtotal,
                request.MaxOrderSubtotal,
                request.FreeShippingThreshold),
            ShippingRateType.QuantityBased => ShippingRate.CreateQuantityBased(
                request.StoreId,
                request.ShippingMethodId,
                request.ShippingZoneId,
                request.CurrencyCode,
                request.BasePrice,
                request.PricePerQuantityUnit ?? 0m,
                request.FreeShippingThreshold),
            _ => throw new ArgumentOutOfRangeException(nameof(request.RateType))
        };

        await repository.AddRateAsync(rate, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapRateDetail(rate));
    }

    public async Task<Result<ShippingRateDetailDto>> UpdateRateAsync(int id, UpdateShippingRateRequest request, CancellationToken cancellationToken = default)
    {
        var rate = await repository.GetRateByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (rate is null || rate.IsDeleted)
        {
            return Result.Failure<ShippingRateDetailDto>(Error.NotFound($"Shipping rate '{id}' was not found."));
        }

        rate.Update(
            request.BasePrice,
            request.PricePerWeightUnit,
            request.PricePerQuantityUnit,
            request.OrderSubtotalPercentage,
            request.FreeShippingThreshold,
            request.MinOrderSubtotal,
            request.MaxOrderSubtotal,
            request.MinWeightGrams,
            request.MaxWeightGrams,
            request.IsActive);

        await repository.SaveRateAsync(rate, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapRateDetail(rate));
    }

    public async Task<Result> DeleteRateAsync(int id, CancellationToken cancellationToken = default)
    {
        var rate = await repository.GetRateByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (rate is null || rate.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Shipping rate '{id}' was not found."));
        }

        rate.SoftDelete();
        await repository.SaveRateAsync(rate, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<ShippingSettingsDto>> GetSettingsAsync(int? storeId, CancellationToken cancellationToken = default) =>
        Result.Success(await BuildSettingsDtoAsync(storeId, cancellationToken).ConfigureAwait(false));

    public async Task<Result<ShippingSettingsDto>> UpdateSettingsAsync(
        int? storeId,
        UpdateShippingSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await settingService.SetAsync(ShippingSettingKeys.Enabled, request.Enabled.ToString(), storeId, cancellationToken).ConfigureAwait(false);
        await settingService.SetAsync(ShippingSettingKeys.DefaultEstimatedDeliveryDays, request.DefaultEstimatedDeliveryDays.ToString(), storeId, cancellationToken).ConfigureAwait(false);
        await settingService.SetAsync(ShippingSettingKeys.AllowFreeShipping, request.AllowFreeShipping.ToString(), storeId, cancellationToken).ConfigureAwait(false);
        await settingService.SetAsync(ShippingSettingKeys.RequireShippingAddress, request.RequireShippingAddress.ToString(), storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success(await BuildSettingsDtoAsync(storeId, cancellationToken).ConfigureAwait(false));
    }

    private async Task<ShippingSettingsDto> BuildSettingsDtoAsync(int? storeId, CancellationToken cancellationToken)
    {
        var enabled = await shippingSettings.IsEnabledAsync(storeId, cancellationToken).ConfigureAwait(false);
        var deliveryDays = await shippingSettings.GetDefaultEstimatedDeliveryDaysAsync(storeId, cancellationToken).ConfigureAwait(false);
        var allowFree = await settingService.GetAsync<bool?>(ShippingSettingKeys.AllowFreeShipping, storeId, cancellationToken).ConfigureAwait(false) ?? true;
        var requireAddress = await settingService.GetAsync<bool?>(ShippingSettingKeys.RequireShippingAddress, storeId, cancellationToken).ConfigureAwait(false) ?? true;
        return new ShippingSettingsDto(enabled, deliveryDays, allowFree, requireAddress);
    }

    private static void ApplyZoneRules(
        ShippingZone zone,
        IReadOnlyList<ShippingZoneCountryDto> countries,
        IReadOnlyList<ShippingZoneStateDto> states,
        IReadOnlyList<ShippingZonePostalRuleDto> postalRules)
    {
        zone.ReplaceCountries(countries.Select(c => ShippingZoneCountry.Create(zone.Id > 0 ? zone.Id : 0, c.CountryCode)));
        zone.ReplaceStates(states.Select(s => ShippingZoneState.Create(zone.Id > 0 ? zone.Id : 0, s.CountryCode, s.StateProvince)));
        zone.ReplacePostalRules(postalRules.Select(r => r.RuleType switch
        {
            PostalRuleType.Exact => ShippingZonePostalRule.CreateExact(zone.Id > 0 ? zone.Id : 0, r.CountryCode, r.PostalFrom),
            PostalRuleType.Prefix => ShippingZonePostalRule.CreatePrefix(zone.Id > 0 ? zone.Id : 0, r.CountryCode, r.PostalFrom),
            PostalRuleType.Range => ShippingZonePostalRule.CreateRange(zone.Id > 0 ? zone.Id : 0, r.CountryCode, r.PostalFrom, r.PostalTo ?? r.PostalFrom),
            _ => throw new ArgumentOutOfRangeException(nameof(r.RuleType))
        }));
    }

    private static ShippingMethodSummaryDto MapMethodSummary(ShippingMethod m) =>
        new(m.Id, m.StoreId, m.Name, m.SystemName, m.ProviderSystemName, m.IsActive, m.DisplayOrder);

    private static ShippingMethodDetailDto MapMethodDetail(ShippingMethod m) =>
        new(m.Id, m.StoreId, m.Name, m.SystemName, m.Description, m.ProviderSystemName, m.IsActive, m.DisplayOrder,
            m.RequiresAddress, m.SupportsTracking, m.EstimatedDeliveryDaysMin, m.EstimatedDeliveryDaysMax,
            m.CreatedAtUtc, m.UpdatedAtUtc);

    private static ShippingZoneSummaryDto MapZoneSummary(ShippingZone z) =>
        new(z.Id, z.StoreId, z.Name, z.SystemName, z.IsDefault, z.IsActive, z.DisplayOrder);

    private static ShippingZoneDetailDto MapZoneDetail(ShippingZone z) =>
        new(z.Id, z.StoreId, z.Name, z.SystemName, z.IsDefault, z.IsActive, z.DisplayOrder,
            z.Countries.Select(c => new ShippingZoneCountryDto(c.CountryCode)).ToList(),
            z.States.Select(s => new ShippingZoneStateDto(s.CountryCode, s.StateProvince)).ToList(),
            z.PostalRules.Select(r => new ShippingZonePostalRuleDto(r.CountryCode, r.RuleType, r.PostalFrom, r.PostalTo)).ToList(),
            z.CreatedAtUtc, z.UpdatedAtUtc);

    private static ShippingRateSummaryDto MapRateSummary(ShippingRate r) =>
        new(r.Id, r.StoreId, r.ShippingMethodId, r.ShippingZoneId, r.CurrencyCode, r.RateType, r.BasePrice, r.IsActive);

    private static ShippingRateDetailDto MapRateDetail(ShippingRate r) =>
        new(r.Id, r.StoreId, r.ShippingMethodId, r.ShippingZoneId, r.CurrencyCode, r.RateType, r.BasePrice,
            r.PricePerWeightUnit, r.PricePerQuantityUnit, r.OrderSubtotalPercentage, r.FreeShippingThreshold,
            r.MinOrderSubtotal, r.MaxOrderSubtotal, r.MinWeightGrams, r.MaxWeightGrams, r.IsActive,
            r.CreatedAtUtc, r.UpdatedAtUtc);
}
