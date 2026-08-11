using Microsoft.AspNetCore.Identity;

namespace Commerce.Framework.Data.Identity;

public sealed class CommerceIdentityUser : IdentityUser
{
    public string? DisplayName { get; set; }
}

public sealed class CommerceIdentityRole : IdentityRole
{
    public string? Description { get; set; }
}
