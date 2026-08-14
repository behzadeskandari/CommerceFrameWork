using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Seo;
using Commerce.Seo.Application.Abstractions;
using Commerce.Seo.Contracts.Admin;
using Commerce.Seo.Domain.Entities;

namespace Commerce.Seo.Application.Admin;

public sealed class SeoAdminService(ISeoRepository repository) : ISeoAdminService
{
    public async Task<Result<IReadOnlyList<UrlRecordDto>>> ListUrlRecordsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var items = await repository.ListUrlRecordsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<UrlRecordDto>>(items.Select(MapUrl).ToList());
    }

    public async Task<Result<UrlRecordDto>> UpsertUrlRecordAsync(UpsertUrlRecordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var slug = SlugNormalizer.Normalize(request.Slug);
            var existing = await repository
                .GetUrlRecordAsync(request.EntityName, request.EntityId, request.LanguageId, request.StoreId, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                existing = UrlRecord.Create(request.EntityName, request.EntityId, slug, request.LanguageId, request.StoreId, request.IsActive);
                await repository.AddUrlRecordAsync(existing, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                existing.Update(slug, request.IsActive);
                await repository.SaveUrlRecordAsync(existing, cancellationToken).ConfigureAwait(false);
            }

            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(MapUrl(existing));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<UrlRecordDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<SeoMetadataDto>> GetMetadataAsync(string entityName, int entityId, int? languageId, int? storeId, CancellationToken cancellationToken = default)
    {
        var metadata = await repository.GetMetadataAsync(entityName, entityId, languageId, storeId, cancellationToken).ConfigureAwait(false);
        if (metadata is null)
        {
            return Result.Failure<SeoMetadataDto>(Error.NotFound("SEO metadata not found."));
        }

        return Result.Success(MapMetadata(metadata));
    }

    public async Task<Result<SeoMetadataDto>> UpsertMetadataAsync(UpsertSeoMetadataRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await repository
            .GetMetadataAsync(request.EntityName, request.EntityId, request.LanguageId, request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            existing = SeoMetadata.Create(
                request.EntityName,
                request.EntityId,
                request.LanguageId,
                request.StoreId,
                request.MetaTitle,
                request.MetaDescription,
                request.MetaKeywords,
                request.CanonicalUrl,
                request.StructuredDataJson);
            await repository.AddMetadataAsync(existing, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.Update(
                request.MetaTitle,
                request.MetaDescription,
                request.MetaKeywords,
                request.CanonicalUrl,
                request.StructuredDataJson);
            await repository.SaveMetadataAsync(existing, cancellationToken).ConfigureAwait(false);
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(MapMetadata(existing));
    }

    public async Task<Result<SeoSettingsDto>> GetSettingsAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (settings is null)
        {
            settings = SeoSettings.CreateDefault(storeId);
            await repository.AddSettingsAsync(settings, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(MapSettings(settings));
    }

    public async Task<Result<SeoSettingsDto>> UpdateSettingsAsync(int storeId, UpdateSeoSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (settings is null)
        {
            settings = SeoSettings.CreateDefault(storeId);
            await repository.AddSettingsAsync(settings, cancellationToken).ConfigureAwait(false);
        }

        settings.Update(request.DefaultMetaTitle, request.DefaultMetaDescription, request.RobotsTxt, request.SitemapEnabled);
        await repository.SaveSettingsAsync(settings, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(MapSettings(settings));
    }

    private static UrlRecordDto MapUrl(UrlRecord record) =>
        new(record.Id, record.EntityName, record.EntityId, record.Slug, record.LanguageId, record.StoreId, record.IsActive);

    private static SeoMetadataDto MapMetadata(SeoMetadata metadata) =>
        new(metadata.Id, metadata.EntityName, metadata.EntityId, metadata.LanguageId, metadata.StoreId,
            metadata.MetaTitle, metadata.MetaDescription, metadata.MetaKeywords, metadata.CanonicalUrl, metadata.StructuredDataJson);

    private static SeoSettingsDto MapSettings(SeoSettings settings) =>
        new(settings.StoreId, settings.DefaultMetaTitle, settings.DefaultMetaDescription, settings.RobotsTxt, settings.SitemapEnabled);
}
