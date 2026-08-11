namespace Commerce.Framework.Data.Entities;

public sealed class BootstrapStore
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string? Hosts { get; set; }

    public bool IsActive { get; set; }
}
