using Commerce.Framework.Core.Errors;

using Commerce.Framework.Core.Results;

using Commerce.Tax.Application.Abstractions;

using Commerce.Tax.Contracts.Admin;

using Commerce.Tax.Domain.Entities;

using Commerce.Tax.Domain.Enums;



namespace Commerce.Tax.Application.Tax;



public sealed class TaxAdminService(ITaxRepository repository) : ITaxAdminService

{

    public async Task<Result<IReadOnlyList<TaxCategorySummaryDto>>> ListCategoriesAsync(int? storeId, CancellationToken cancellationToken = default)

    {

        var categories = await repository.ListCategoriesAsync(storeId, cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<TaxCategorySummaryDto>>(

            categories.Where(c => !c.IsDeleted).Select(MapCategorySummary).ToList());

    }



    public async Task<Result<TaxCategoryDetailDto>> GetCategoryAsync(int id, CancellationToken cancellationToken = default)

    {

        var category = await repository.GetCategoryByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return category is null || category.IsDeleted

            ? Result.Failure<TaxCategoryDetailDto>(Error.NotFound($"Tax category '{id}' was not found."))

            : Result.Success(MapCategoryDetail(category));

    }



    public async Task<Result<TaxCategoryDetailDto>> CreateCategoryAsync(CreateTaxCategoryRequest request, CancellationToken cancellationToken = default)

    {

        ArgumentNullException.ThrowIfNull(request);

        var category = TaxCategory.Create(

            request.StoreId,

            request.Name,

            request.SystemName,

            request.Description,

            request.IsExempt,

            request.IsActive,

            request.DisplayOrder);



        await repository.AddCategoryAsync(category, cancellationToken).ConfigureAwait(false);

        return Result.Success(MapCategoryDetail(category));

    }



    public async Task<Result<TaxCategoryDetailDto>> UpdateCategoryAsync(int id, UpdateTaxCategoryRequest request, CancellationToken cancellationToken = default)

    {

        var category = await repository.GetCategoryByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (category is null || category.IsDeleted)

        {

            return Result.Failure<TaxCategoryDetailDto>(Error.NotFound($"Tax category '{id}' was not found."));

        }



        category.Update(

            request.Name,

            request.Description,

            request.IsExempt,

            request.IsActive,

            request.DisplayOrder);



        await repository.SaveCategoryAsync(category, cancellationToken).ConfigureAwait(false);

        return Result.Success(MapCategoryDetail(category));

    }



    public async Task<Result> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)

    {

        var category = await repository.GetCategoryByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (category is null || category.IsDeleted)

        {

            return Result.Failure(Error.NotFound($"Tax category '{id}' was not found."));

        }



        category.SoftDelete();

        await repository.SaveCategoryAsync(category, cancellationToken).ConfigureAwait(false);

        return Result.Success();

    }



    public async Task<Result<IReadOnlyList<TaxZoneSummaryDto>>> ListZonesAsync(int? storeId, CancellationToken cancellationToken = default)

    {

        var zones = await repository.ListZonesAsync(storeId, cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<TaxZoneSummaryDto>>(

            zones.Where(z => !z.IsDeleted).Select(MapZoneSummary).ToList());

    }



    public async Task<Result<TaxZoneDetailDto>> GetZoneAsync(int id, CancellationToken cancellationToken = default)

    {

        var zone = await repository.GetZoneByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return zone is null || zone.IsDeleted

            ? Result.Failure<TaxZoneDetailDto>(Error.NotFound($"Tax zone '{id}' was not found."))

            : Result.Success(MapZoneDetail(zone));

    }



    public async Task<Result<TaxZoneDetailDto>> CreateZoneAsync(CreateTaxZoneRequest request, CancellationToken cancellationToken = default)

    {

        var zone = TaxZone.Create(

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



    public async Task<Result<TaxZoneDetailDto>> UpdateZoneAsync(int id, UpdateTaxZoneRequest request, CancellationToken cancellationToken = default)

    {

        var zone = await repository.GetZoneByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (zone is null || zone.IsDeleted)

        {

            return Result.Failure<TaxZoneDetailDto>(Error.NotFound($"Tax zone '{id}' was not found."));

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

            return Result.Failure(Error.NotFound($"Tax zone '{id}' was not found."));

        }



        zone.SoftDelete();

        await repository.SaveZoneAsync(zone, cancellationToken).ConfigureAwait(false);

        return Result.Success();

    }



    public async Task<Result<IReadOnlyList<TaxRateSummaryDto>>> ListRatesAsync(int? storeId, int? categoryId, CancellationToken cancellationToken = default)

    {

        var rates = await repository.ListRatesAsync(storeId, categoryId, cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<TaxRateSummaryDto>>(

            rates.Where(r => !r.IsDeleted).Select(MapRateSummary).ToList());

    }



    public async Task<Result<TaxRateDetailDto>> GetRateAsync(int id, CancellationToken cancellationToken = default)

    {

        var rate = await repository.GetRateByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return rate is null || rate.IsDeleted

            ? Result.Failure<TaxRateDetailDto>(Error.NotFound($"Tax rate '{id}' was not found."))

            : Result.Success(MapRateDetail(rate));

    }



    public async Task<Result<TaxRateDetailDto>> CreateRateAsync(CreateTaxRateRequest request, CancellationToken cancellationToken = default)

    {

        var rate = TaxRate.CreatePercentage(

            request.StoreId,

            request.TaxCategoryId,

            request.TaxZoneId,

            request.Percentage,

            request.TaxShipping,

            request.Priority,

            request.EffectiveFromUtc,

            request.EffectiveToUtc);



        await repository.AddRateAsync(rate, cancellationToken).ConfigureAwait(false);

        return Result.Success(MapRateDetail(rate));

    }



    public async Task<Result<TaxRateDetailDto>> UpdateRateAsync(int id, UpdateTaxRateRequest request, CancellationToken cancellationToken = default)

    {

        var rate = await repository.GetRateByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (rate is null || rate.IsDeleted)

        {

            return Result.Failure<TaxRateDetailDto>(Error.NotFound($"Tax rate '{id}' was not found."));

        }



        rate.Update(

            request.Percentage,

            request.TaxShipping,

            request.Priority,

            request.EffectiveFromUtc,

            request.EffectiveToUtc,

            request.IsActive);



        await repository.SaveRateAsync(rate, cancellationToken).ConfigureAwait(false);

        return Result.Success(MapRateDetail(rate));

    }



    public async Task<Result> DeleteRateAsync(int id, CancellationToken cancellationToken = default)

    {

        var rate = await repository.GetRateByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (rate is null || rate.IsDeleted)

        {

            return Result.Failure(Error.NotFound($"Tax rate '{id}' was not found."));

        }



        rate.SoftDelete();

        await repository.SaveRateAsync(rate, cancellationToken).ConfigureAwait(false);

        return Result.Success();

    }



    private static void ApplyZoneRules(

        TaxZone zone,

        IReadOnlyList<TaxZoneCountryDto> countries,

        IReadOnlyList<TaxZoneStateDto> states,

        IReadOnlyList<TaxZonePostalRuleDto> postalRules)

    {

        var zoneId = zone.Id > 0 ? zone.Id : 0;

        zone.ReplaceCountries(countries.Select(c => TaxZoneCountry.Create(zoneId, c.CountryCode)));

        zone.ReplaceStates(states.Select(s => TaxZoneState.Create(zoneId, s.CountryCode, s.StateProvince)));

        zone.ReplacePostalRules(postalRules.Select(r => r.RuleType switch

        {

            PostalRuleType.Exact => TaxZonePostalRule.CreateExact(zoneId, r.CountryCode, r.PostalFrom),

            PostalRuleType.Prefix => TaxZonePostalRule.CreatePrefix(zoneId, r.CountryCode, r.PostalFrom),

            PostalRuleType.Range => TaxZonePostalRule.CreateRange(zoneId, r.CountryCode, r.PostalFrom, r.PostalTo ?? r.PostalFrom),

            _ => throw new ArgumentOutOfRangeException(nameof(r.RuleType))

        }));

    }



    private static TaxCategorySummaryDto MapCategorySummary(TaxCategory c) =>

        new(c.Id, c.StoreId, c.Name, c.SystemName, c.IsExempt, c.IsActive, c.DisplayOrder);



    private static TaxCategoryDetailDto MapCategoryDetail(TaxCategory c) =>

        new(c.Id, c.StoreId, c.Name, c.SystemName, c.Description, c.IsExempt, c.IsActive, c.DisplayOrder,

            c.CreatedAtUtc, c.UpdatedAtUtc);



    private static TaxZoneSummaryDto MapZoneSummary(TaxZone z) =>

        new(z.Id, z.StoreId, z.Name, z.SystemName, z.IsDefault, z.IsActive, z.DisplayOrder);



    private static TaxZoneDetailDto MapZoneDetail(TaxZone z) =>

        new(z.Id, z.StoreId, z.Name, z.SystemName, z.IsDefault, z.IsActive, z.DisplayOrder,

            z.Countries.Select(c => new TaxZoneCountryDto(c.CountryCode)).ToList(),

            z.States.Select(s => new TaxZoneStateDto(s.CountryCode, s.StateProvince)).ToList(),

            z.PostalRules.Select(r => new TaxZonePostalRuleDto(r.CountryCode, r.RuleType, r.PostalFrom, r.PostalTo)).ToList(),

            z.CreatedAtUtc, z.UpdatedAtUtc);



    private static TaxRateSummaryDto MapRateSummary(TaxRate r) =>

        new(r.Id, r.StoreId, r.TaxCategoryId, r.TaxZoneId, r.RateType, r.Percentage, r.TaxShipping, r.Priority, r.IsActive);



    private static TaxRateDetailDto MapRateDetail(TaxRate r) =>

        new(r.Id, r.StoreId, r.TaxCategoryId, r.TaxZoneId, r.RateType, r.Percentage, r.FixedAmount, r.TaxShipping,

            r.Priority, r.EffectiveFromUtc, r.EffectiveToUtc, r.IsActive, r.CreatedAtUtc, r.UpdatedAtUtc);

}

