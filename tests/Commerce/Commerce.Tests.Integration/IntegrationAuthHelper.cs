using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Commerce.Tests.Integration;

internal static class IntegrationAuthHelper
{
    internal const string AdminEmail = "admin@example.com";
    internal const string AdminPassword = "Password123!";

    internal static async Task LoginAsAdministratorAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/customers/login", new
        {
            Email = AdminEmail,
            Password = AdminPassword,
            RememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    internal static async Task RegisterAndLoginCustomerAsync(
        HttpClient client,
        string email,
        string password,
        string firstName = "Test",
        string lastName = "Customer")
    {
        var register = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
    }
}
