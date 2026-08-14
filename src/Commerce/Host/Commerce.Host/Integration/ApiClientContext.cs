using Commerce.Integration.Contracts.ApiClients;

namespace Commerce.Host.Integration;

public static class ApiClientContextKeys
{
    public const string Authentication = "Commerce.ApiClient.Authentication";
}

public static class HttpContextApiClientExtensions
{
    public static ApiClientAuthenticationResult? GetApiClientAuthentication(this HttpContext context) =>
        context.Items.TryGetValue(ApiClientContextKeys.Authentication, out var value)
            ? value as ApiClientAuthenticationResult
            : null;
}
