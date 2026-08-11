namespace Commerce.Framework.Data.Entities;

public sealed class BootstrapCurrency
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string CurrencyCode { get; set; } = null!;

    public decimal Rate { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsPublished { get; set; }
}
