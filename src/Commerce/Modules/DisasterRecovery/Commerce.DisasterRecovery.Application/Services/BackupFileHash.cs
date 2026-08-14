using System.Security.Cryptography;

namespace Commerce.DisasterRecovery.Application.Services;

public static class BackupFileHash
{
    public static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
