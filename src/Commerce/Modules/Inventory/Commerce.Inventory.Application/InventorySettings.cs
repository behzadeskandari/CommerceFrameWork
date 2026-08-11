namespace Commerce.Inventory.Application;

public sealed class InventorySettings
{
    public int DefaultReservationDays { get; set; } = 7;

    public TimeSpan DefaultReservationDuration => TimeSpan.FromDays(DefaultReservationDays);
}
