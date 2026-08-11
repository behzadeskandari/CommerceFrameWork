namespace Commerce.Orders.Domain.Entities;

public sealed class StoreOrderNumberSequence : Commerce.Framework.Core.Entities.Entity
{
    private StoreOrderNumberSequence()
    {
    }

    public int StoreId { get; private set; }

    public int Year { get; private set; }

    public int LastSequenceNumber { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static StoreOrderNumberSequence Create(int storeId, int year) =>
        new()
        {
            StoreId = storeId,
            Year = year,
            LastSequenceNumber = 0
        };

    public int Next()
    {
        LastSequenceNumber++;
        return LastSequenceNumber;
    }
}
