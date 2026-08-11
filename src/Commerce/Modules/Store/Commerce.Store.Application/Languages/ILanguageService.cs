using Commerce.Framework.Core.Results;
using Commerce.Store.Application.Languages;
using Commerce.Store.Contracts.Languages;

namespace Commerce.Store.Application.Languages;

public interface ILanguageService
{
    Task<Result<LanguageDto>> CreateAsync(
        CreateLanguageRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<LanguageDto>> UpdateAsync(
        int languageId,
        UpdateLanguageRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<LanguageDto>> GetByIdAsync(int languageId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LanguageDto>>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
