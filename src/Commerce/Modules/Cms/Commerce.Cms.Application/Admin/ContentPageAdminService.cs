using Commerce.Cms.Application.Abstractions;
using Commerce.Cms.Application.Security;
using Commerce.Cms.Contracts.Admin;
using Commerce.Cms.Domain.Entities;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Cms.Application.Admin;

public sealed class ContentPageAdminService(ICmsRepository repository, IContentHtmlSanitizer sanitizer) : IContentPageAdminService
{
    public async Task<Result<IReadOnlyList<ContentPageSummaryDto>>> ListAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var pages = await repository.ListPagesAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<ContentPageSummaryDto>>(pages.Select(MapSummary).ToList());
    }

    public async Task<Result<ContentPageDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var page = await repository.GetPageByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return page is null
            ? Result.Failure<ContentPageDetailDto>(Error.NotFound($"Content page '{id}' was not found."))
            : Result.Success(MapDetail(page));
    }

    public async Task<Result<ContentPageDetailDto>> CreateAsync(CreateContentPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Localizations.Count == 0)
        {
            return Result.Failure<ContentPageDetailDto>(Error.Validation("At least one localization is required."));
        }

        foreach (var loc in request.Localizations)
        {
            if (await repository.PageSlugExistsAsync(request.StoreId, loc.LanguageId, ContentPageLocalization.NormalizeSlug(loc.Slug), null, cancellationToken).ConfigureAwait(false))
            {
                return Result.Failure<ContentPageDetailDto>(Error.Validation($"Slug '{loc.Slug}' already exists for this store and language."));
            }
        }

        var page = ContentPage.Create(request.StoreId, request.SystemName, request.IsPublished, request.PublishedFromUtc, request.PublishedToUtc);
        foreach (var loc in request.Localizations)
        {
            page.AddLocalization(
                loc.LanguageId,
                loc.Title,
                loc.Slug,
                sanitizer.Sanitize(loc.Body),
                loc.MetaTitle,
                loc.MetaDescription,
                loc.MetaKeywords,
                loc.CanonicalUrl);
        }

        await repository.AddPageAsync(page, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(page));
    }

    public async Task<Result<ContentPageDetailDto>> UpdateAsync(int id, UpdateContentPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var page = await repository.GetPageByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (page is null)
        {
            return Result.Failure<ContentPageDetailDto>(Error.NotFound($"Content page '{id}' was not found."));
        }

        foreach (var loc in request.Localizations)
        {
            if (await repository.PageSlugExistsAsync(page.StoreId, loc.LanguageId, ContentPageLocalization.NormalizeSlug(loc.Slug), page.Id, cancellationToken).ConfigureAwait(false))
            {
                return Result.Failure<ContentPageDetailDto>(Error.Validation($"Slug '{loc.Slug}' already exists for this store and language."));
            }
        }

        page.Update(request.SystemName, request.IsPublished, request.PublishedFromUtc, request.PublishedToUtc);
        var localizations = request.Localizations.Select(loc =>
            ContentPageLocalization.Create(
                page.Id,
                loc.LanguageId,
                loc.Title,
                loc.Slug,
                sanitizer.Sanitize(loc.Body),
                loc.MetaTitle,
                loc.MetaDescription,
                loc.MetaKeywords,
                loc.CanonicalUrl)).ToList();
        page.ReplaceLocalizations(localizations);
        await repository.SavePageAsync(page, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(page));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var page = await repository.GetPageByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (page is null)
        {
            return Result.Failure(Error.NotFound($"Content page '{id}' was not found."));
        }

        await repository.DeletePageAsync(page, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var page = await repository.GetPageByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (page is null)
        {
            return Result.Failure(Error.NotFound($"Content page '{id}' was not found."));
        }

        page.Update(page.SystemName, isPublished: true, page.PublishedFromUtc, page.PublishedToUtc);
        await repository.SavePageAsync(page, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> UnpublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var page = await repository.GetPageByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (page is null)
        {
            return Result.Failure(Error.NotFound($"Content page '{id}' was not found."));
        }

        page.Update(page.SystemName, isPublished: false, page.PublishedFromUtc, page.PublishedToUtc);
        await repository.SavePageAsync(page, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static ContentPageSummaryDto MapSummary(ContentPage page)
    {
        var first = page.Localizations.FirstOrDefault();
        return new ContentPageSummaryDto(
            page.Id,
            page.StoreId,
            page.SystemName,
            page.IsPublished,
            page.PublishedFromUtc,
            page.PublishedToUtc,
            first?.Title,
            first?.Slug,
            page.UpdatedAtUtc);
    }

    private static ContentPageDetailDto MapDetail(ContentPage page) =>
        new(
            page.Id,
            page.StoreId,
            page.SystemName,
            page.IsPublished,
            page.PublishedFromUtc,
            page.PublishedToUtc,
            page.Localizations.Select(x => new ContentPageLocalizationDto(
                x.Id, x.LanguageId, x.Title, x.Slug, x.Body, x.MetaTitle, x.MetaDescription, x.MetaKeywords, x.CanonicalUrl)).ToList(),
            page.CreatedAtUtc,
            page.UpdatedAtUtc);
}
