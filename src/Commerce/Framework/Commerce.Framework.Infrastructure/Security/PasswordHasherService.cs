using Commerce.Framework.Contracts.Security;
using Microsoft.AspNetCore.Identity;

namespace Commerce.Framework.Infrastructure.Security;

internal sealed class BootstrapUser;

public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<BootstrapUser> _hasher = new();

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return _hasher.HashPassword(null!, password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword);

        var result = _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
