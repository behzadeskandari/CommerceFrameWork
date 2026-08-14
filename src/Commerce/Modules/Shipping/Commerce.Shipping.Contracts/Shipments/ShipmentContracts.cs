using Commerce.Framework.Core.Results;
using Commerce.Shipping.Contracts.Shipping;
using Commerce.Shipping.Domain.Enums;

namespace Commerce.Shipping.Contracts.Shipments;

public sealed record ShipmentItemDto(
    int Id,
    int OrderItemId,
    int OfferId,
    int ProductId,
    int Quantity);

public sealed record ShipmentSummaryDto(
    int Id,
    int OrderId,
    int StoreId,
    ShipmentStatus Status,
    string? TrackingNumber,
    string? CarrierName,
    DateTime? ShippedAtUtc,
    DateTime CreatedAtUtc);

public sealed record ShipmentDetailDto(
    int Id,
    int OrderId,
    int StoreId,
    int? ShippingMethodId,
    string? ProviderSystemName,
    ShipmentStatus Status,
    string? TrackingNumber,
    string? TrackingUrl,
    string? CarrierName,
    string? Notes,
    DateTime? ShippedAtUtc,
    DateTime? DeliveredAtUtc,
    IReadOnlyList<ShipmentItemDto> Items,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateShipmentRequest(
    int OrderId,
    int? ShippingMethodId,
    string? ProviderSystemName,
    string? Notes,
    IReadOnlyList<CreateShipmentItemRequest> Items);

public sealed record CreateShipmentItemRequest(
    int OrderItemId,
    int OfferId,
    int ProductId,
    int Quantity);

public sealed record UpdateShipmentTrackingRequest(
    string? TrackingNumber,
    string? TrackingUrl,
    string? CarrierName);

public sealed record ShippingSettingsDto(
    bool Enabled,
    int DefaultEstimatedDeliveryDays,
    bool AllowFreeShipping,
    bool RequireShippingAddress);

public sealed record UpdateShippingSettingsRequest(
    bool Enabled,
    int DefaultEstimatedDeliveryDays,
    bool AllowFreeShipping,
    bool RequireShippingAddress);

public interface IShipmentAdminService
{
    Task<Result<IReadOnlyList<ShipmentSummaryDto>>> ListByOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<Result<ShipmentDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<ShipmentDetailDto>> CreateAsync(CreateShipmentRequest request, CancellationToken cancellationToken = default);

    Task<Result<ShipmentDetailDto>> UpdateTrackingAsync(int id, UpdateShipmentTrackingRequest request, CancellationToken cancellationToken = default);

    Task<Result<ShipmentDetailDto>> MarkShippedAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<ShipmentDetailDto>> MarkDeliveredAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> CancelOpenShipmentsForOrderAsync(int orderId, string reason, CancellationToken cancellationToken = default);
}

public interface IShippingProviderRegistry
{
    IReadOnlyList<ShippingProviderDescriptorDto> ListProviders();
}
