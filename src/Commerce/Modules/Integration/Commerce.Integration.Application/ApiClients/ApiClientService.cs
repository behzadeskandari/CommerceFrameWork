using System.Security.Cryptography;
using System.Text;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Integration.Application.Abstractions;
using Commerce.Integration.Contracts.ApiClients;
using Commerce.Integration.Domain.Entities;

namespace Commerce.Integration.Application.ApiClients;

internal static class ApiClientMapper
{
    internal static ApiClientSummaryDto MapSummary(ApiClient client) =>
        new(
            client.Id,
            client.StoreId,
            client.Name,
            client.KeyPrefix,
            client.Scopes.ToList(),
            client.IsCurrentlyActive(),
            client.CreatedAtUtc);

    internal static ApiClientDetailDto MapDetail(ApiClient client) =>
        new(
            client.Id,
            client.StoreId,
            client.Name,
            client.KeyPrefix,
            client.Scopes.ToList(),
            client.IsCurrentlyActive(),
            client.CreatedAtUtc,
            client.UpdatedAtUtc);
}

public sealed class ApiClientAdminService(IIntegrationRepository repository) : IApiClientAdminService
{
    public async Task<Result<IReadOnlyList<ApiClientSummaryDto>>> ListAsync(
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var clients = await repository.ListApiClientsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<ApiClientSummaryDto>>(
            clients.Select(ApiClientMapper.MapSummary).ToList());
    }

    public async Task<Result<ApiClientDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = await repository.GetApiClientByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return client is null
            ? Result.Failure<ApiClientDetailDto>(Error.NotFound("API client not found."))
            : Result.Success(ApiClientMapper.MapDetail(client));
    }

    public async Task<Result<(ApiClientDetailDto Client, string ApiKey)>> CreateAsync(
        CreateApiClientRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (prefix, secret, fullKey) = GenerateApiKey();
        var hash = HashKey(fullKey);
        var client = ApiClient.Create(request.StoreId, request.Name, prefix, hash, request.Scopes);
        await repository.AddApiClientAsync(client, cancellationToken).ConfigureAwait(false);
        return Result.Success((ApiClientMapper.MapDetail(client), fullKey));
    }

    public async Task<Result<ApiClientDetailDto>> UpdateAsync(
        int id,
        UpdateApiClientRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = await repository.GetApiClientByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return Result.Failure<ApiClientDetailDto>(Error.NotFound("API client not found."));
        }

        client.Update(request.Name, request.Scopes);
        await repository.UpdateApiClientAsync(client, cancellationToken).ConfigureAwait(false);
        return Result.Success(ApiClientMapper.MapDetail(client));
    }

    public async Task<Result> RevokeAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = await repository.GetApiClientByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return Result.Failure(Error.NotFound("API client not found."));
        }

        client.Revoke();
        await repository.UpdateApiClientAsync(client, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = await repository.GetApiClientByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return Result.Failure(Error.NotFound("API client not found."));
        }

        client.SoftDelete();
        await repository.UpdateApiClientAsync(client, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    internal static (string Prefix, string Secret, string FullKey) GenerateApiKey()
    {
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', 'x')
            .Replace('/', 'y');

        var prefix = secret[..8];
        var fullKey = $"ck_{prefix}_{secret}";
        return (prefix, secret, fullKey);
    }

    internal static string HashKey(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public sealed class ApiClientAuthenticator(IIntegrationRepository repository) : IApiClientAuthenticator
{
    public async Task<ApiClientAuthenticationResult> AuthenticateAsync(
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith("ck_", StringComparison.Ordinal))
        {
            return new ApiClientAuthenticationResult(false, null, null, [], "Invalid API key format.");
        }

        var parts = apiKey.Split('_', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return new ApiClientAuthenticationResult(false, null, null, [], "Invalid API key format.");
        }

        var prefix = parts[1];
        var client = await repository.GetApiClientByPrefixAsync(prefix, cancellationToken).ConfigureAwait(false);
        if (client is null || !client.IsCurrentlyActive())
        {
            return new ApiClientAuthenticationResult(false, null, null, [], "API client not found or revoked.");
        }

        var hash = ApiClientAdminService.HashKey(apiKey);
        if (!FixedTimeEquals(hash, client.KeyHash))
        {
            return new ApiClientAuthenticationResult(false, null, null, [], "Invalid API key.");
        }

        return new ApiClientAuthenticationResult(
            true,
            client.Id,
            client.StoreId,
            client.Scopes.ToList(),
            null);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}
