using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Store.Application.Abstractions;
using Commerce.Store.Contracts.Languages;
using Commerce.Store.Domain.Entities;

namespace Commerce.Store.Application.Languages;

public sealed class LanguageService(ILanguageRepository languageRepository) : ILanguageService
{
    public async Task<Result<LanguageDto>> CreateAsync(
        CreateLanguageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var normalizedCode = request.LanguageCode.Trim().ToLowerInvariant();
            if (await languageRepository.GetByLanguageCodeAsync(normalizedCode, cancellationToken)
                    .ConfigureAwait(false) is not null)
            {
                return Result.Failure<LanguageDto>(
                    Error.Conflict($"Language code '{request.LanguageCode}' already exists."));
            }

            var language = Language.Create(
                request.Name,
                request.LanguageCode,
                request.CultureCode,
                request.NativeName ?? request.Name,
                request.IsRtl,
                request.DisplayOrder,
                request.IsActive);

            await languageRepository.AddAsync(language, cancellationToken).ConfigureAwait(false);
            return Result.Success(Map(language));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<LanguageDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<LanguageDto>> UpdateAsync(
        int languageId,
        UpdateLanguageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var language = await languageRepository.GetByIdAsync(languageId, cancellationToken).ConfigureAwait(false);
        if (language is null)
        {
            return Result.Failure<LanguageDto>(Error.NotFound($"Language '{languageId}' was not found."));
        }

        try
        {
            language.Update(
                request.Name,
                request.CultureCode,
                request.NativeName ?? request.Name,
                request.IsRtl,
                request.DisplayOrder,
                request.IsActive);

            await languageRepository.UpdateAsync(language, cancellationToken).ConfigureAwait(false);
            return Result.Success(Map(language));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<LanguageDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<LanguageDto>> GetByIdAsync(int languageId, CancellationToken cancellationToken = default)
    {
        var language = await languageRepository.GetByIdAsync(languageId, cancellationToken).ConfigureAwait(false);
        if (language is null)
        {
            return Result.Failure<LanguageDto>(Error.NotFound($"Language '{languageId}' was not found."));
        }

        return Result.Success(Map(language));
    }

    public async Task<Result<IReadOnlyList<LanguageDto>>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var languages = await languageRepository.ListAsync(includeInactive, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<LanguageDto>>(languages.Select(Map).ToList());
    }

    internal static LanguageDto Map(Language language) =>
        new(
            language.Id,
            language.Name,
            language.LanguageCode,
            language.CultureCode,
            language.NativeName,
            language.IsActive,
            language.IsRtl,
            language.DisplayOrder,
            language.CreatedAtUtc,
            language.UpdatedAtUtc);
}
