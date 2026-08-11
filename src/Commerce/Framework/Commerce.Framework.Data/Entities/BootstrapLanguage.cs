namespace Commerce.Framework.Data.Entities;

public sealed class BootstrapLanguage
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Culture { get; set; } = null!;

    public bool IsDefault { get; set; }

    public bool IsPublished { get; set; }

    public bool Rtl { get; set; }
}
