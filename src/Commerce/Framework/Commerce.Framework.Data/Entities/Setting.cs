namespace Commerce.Framework.Data.Entities;

public sealed class Setting
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Value { get; set; } = null!;

    public int StoreId { get; set; }

    public string DataType { get; set; } = "string";
}
