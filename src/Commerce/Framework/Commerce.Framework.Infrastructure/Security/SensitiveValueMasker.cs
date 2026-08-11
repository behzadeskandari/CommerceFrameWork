using System.Text.RegularExpressions;

namespace Commerce.Framework.Infrastructure.Security;

public static partial class SensitiveValueMasker
{
    public static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "[empty]";
        }

        var masked = PasswordRegex().Replace(connectionString, "$1=***");
        masked = UserIdRegex().Replace(masked, "$1=***");
        return masked;
    }

    [GeneratedRegex("(Password|Pwd)\\s*=\\s*[^;]*", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordRegex();

    [GeneratedRegex("(User Id|UID)\\s*=\\s*[^;]*", RegexOptions.IgnoreCase)]
    private static partial Regex UserIdRegex();
}
