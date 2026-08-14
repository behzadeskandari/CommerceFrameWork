using Commerce.Framework.Core.Results;

namespace Commerce.Integration.Contracts.ApiClients;

public sealed record ApiClientSummaryDto(
    int Id,
    int? StoreId,
    string Name,
    string KeyPrefix,
    IReadOnlyList<string> Scopes,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record ApiClientDetailDto(
    int Id,
    int? StoreId,
    string Name,
    string KeyPrefix,
    IReadOnlyList<string> Scopes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateApiClientRequest(
    int? StoreId,
    string Name,
    IReadOnlyList<string> Scopes);

public sealed record UpdateApiClientRequest(
    string Name,
    IReadOnlyList<string> Scopes);

public sealed record ApiClientAuthenticationResult(
    bool IsAuthenticated,
    int? ApiClientId,
    int? StoreId,
    IReadOnlyList<string> Scopes,
    string? FailureReason);

public interface IApiClientAdminService
{
    Task<Result<IReadOnlyList<ApiClientSummaryDto>>> ListAsync(
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<Result<ApiClientDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<(ApiClientDetailDto Client, string ApiKey)>> CreateAsync(
        CreateApiClientRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ApiClientDetailDto>> UpdateAsync(
        int id,
        UpdateApiClientRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> RevokeAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IApiClientAuthenticator
{
    Task<ApiClientAuthenticationResult> AuthenticateAsync(
        string? apiKey,
        CancellationToken cancellationToken = default);
}

public static class ApiScopes
{
    public const string OrdersRead = "orders.read";
    public const string ProductsRead = "products.read";
    public const string CustomersRead = "customers.read";
}
