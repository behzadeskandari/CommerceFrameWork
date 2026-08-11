namespace Commerce.Framework.Contracts.Localization;

public interface ILanguageResolver
{
    Task<LanguageResolutionResult?> ResolveAsync(
        int storeId,
        int defaultLanguageId,
        string? acceptLanguageHeader,
        string? preferenceCookie,
        CancellationToken cancellationToken = default);
}

public sealed record LanguageResolutionResult(
    int LanguageId,
    string LanguageCode,
    string CultureCode,
    bool IsRtl);
