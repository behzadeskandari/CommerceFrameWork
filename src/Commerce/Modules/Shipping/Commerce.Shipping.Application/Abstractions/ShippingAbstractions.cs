using Commerce.Shipping.Domain.Entities;

namespace Commerce.Shipping.Application.Abstractions;

public interface IShippingRepository
{
    Task<IReadOnlyList<ShippingMethod>> GetActiveMethodsAsync(int storeId, CancellationToken cancellationToken = default);

    Task<ShippingMethod?> GetMethodByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingMethod>> ListMethodsAsync(int? storeId, CancellationToken cancellationToken = default);

    Task AddMethodAsync(ShippingMethod method, CancellationToken cancellationToken = default);

    Task SaveMethodAsync(ShippingMethod method, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingZone>> GetActiveZonesAsync(int storeId, CancellationToken cancellationToken = default);

    Task<ShippingZone?> GetZoneByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingZone>> ListZonesAsync(int? storeId, CancellationToken cancellationToken = default);

    Task AddZoneAsync(ShippingZone zone, CancellationToken cancellationToken = default);

    Task SaveZoneAsync(ShippingZone zone, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingRate>> GetActiveRatesAsync(int storeId, CancellationToken cancellationToken = default);

    Task<ShippingRate?> GetRateByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingRate>> ListRatesAsync(int? storeId, int? methodId, CancellationToken cancellationToken = default);

    Task AddRateAsync(ShippingRate rate, CancellationToken cancellationToken = default);

    Task SaveRateAsync(ShippingRate rate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Shipment>> ListShipmentsByOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<Shipment?> GetShipmentByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddShipmentAsync(Shipment shipment, CancellationToken cancellationToken = default);

    Task SaveShipmentAsync(Shipment shipment, CancellationToken cancellationToken = default);

    Task<decimal> GetShippedQuantityForOrderItemAsync(int orderItemId, CancellationToken cancellationToken = default);
}
