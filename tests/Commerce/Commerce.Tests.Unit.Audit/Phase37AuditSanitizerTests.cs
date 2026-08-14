using Commerce.Audit.Application.Security;
using System.Text.Json;
using Xunit;

namespace Commerce.Tests.Unit.Audit;

public sealed class Phase37AuditSanitizerTests
{
    [Fact]
    public void SanitizeDetails_MasksPasswordAndSecretKeys()
    {
        var sanitized = AuditSanitizer.SanitizeDetails(new Dictionary<string, string?>
        {
            ["username"] = "admin",
            ["password"] = "SuperSecret123!",
            ["apiKey"] = "sk_live_abc123",
            ["webhookSecret"] = "whsec_xyz"
        });

        Assert.Equal("admin", sanitized["username"]);
        Assert.Equal("***", sanitized["password"]);
        Assert.Equal("***", sanitized["apiKey"]);
        Assert.Equal("***", sanitized["webhookSecret"]);
    }

    [Fact]
    public void SanitizeDetailsJson_MasksNestedSensitiveFields()
    {
        var json = JsonSerializer.Serialize(new
        {
            user = "admin",
            credentials = new
            {
                password = "hidden",
                token = "abc123"
            }
        });

        var sanitized = AuditSanitizer.SanitizeDetailsJson(json);
        Assert.NotNull(sanitized);
        Assert.DoesNotContain("hidden", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("abc123", sanitized, StringComparison.Ordinal);
        Assert.Contains("***", sanitized, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskSensitiveText_RedactsBearerTokens()
    {
        var masked = AuditSanitizer.MaskSensitiveText("Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.payload");
        Assert.Equal("Authorization: Bearer ***", masked);
    }
}
