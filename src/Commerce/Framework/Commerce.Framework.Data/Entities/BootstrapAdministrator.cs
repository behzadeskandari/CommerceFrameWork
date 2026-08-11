namespace Commerce.Framework.Data.Entities;

public sealed class BootstrapAdministrator
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }
}
