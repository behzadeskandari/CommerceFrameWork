using Commerce.Framework.Contracts.Localization;
using Commerce.Store.Application.Abstractions;

namespace Commerce.Store.Infrastructure.Localization;

public sealed class LanguageResolver(ILanguageRepository languageRepository) : ILanguageResolver
{
    public async Task<LanguageResolutionResult?> ResolveAsync(
        int storeId,
        int defaultLanguageId,
        string? acceptLanguageHeader,
        string? preferenceCookie,
        CancellationToken cancellationToken = default)
    {
        _ = storeId;

        if (!string.IsNullOrWhiteSpace(preferenceCookie))
        {
            var fromCookie = await ResolveByCodeAsync(preferenceCookie, cancellationToken).ConfigureAwait(false);
            if (fromCookie is not null)
            {
                return fromCookie;
            }
        }

        if (!string.IsNullOrWhiteSpace(acceptLanguageHeader))
        {
            foreach (var token in ParseAcceptLanguage(acceptLanguageHeader))
            {
                var fromHeader = await ResolveByCodeAsync(token, cancellationToken).ConfigureAwait(false);
                if (fromHeader is not null)
                {
                    return fromHeader;
                }
            }
        }

        return await ResolveByIdAsync(defaultLanguageId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<LanguageResolutionResult?> ResolveByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var normalized = code.Split('-', StringSplitOptions.RemoveEmptyEntries)[0].Trim().ToLowerInvariant();
        var language = await languageRepository.GetByLanguageCodeAsync(normalized, cancellationToken)
            .ConfigureAwait(false);

        if (language is null || !language.IsActive)
        {
            return null;
        }

        return Map(language);
    }

    private async Task<LanguageResolutionResult?> ResolveByIdAsync(
        int languageId,
        CancellationToken cancellationToken)
    {
        var language = await languageRepository.GetByIdAsync(languageId, cancellationToken).ConfigureAwait(false);
        if (language is null || !language.IsActive)
        {
            return null;
        }

        return Map(language);
    }

    private static LanguageResolutionResult Map(Domain.Entities.Language language) =>
        new(language.Id, language.LanguageCode, language.CultureCode, language.IsRtl);

    private static IEnumerable<string> ParseAcceptLanguage(string header)
    {
        foreach (var segment in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var token = segment.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            if (!string.IsNullOrWhiteSpace(token))
            {
                yield return token;
            }
        }
    }
}
