using Commerce.Framework.Core.Results;
using Commerce.Inventory.Domain.Enums;

namespace Commerce.Inventory.Contracts.Inventory;

public sealed record OfferAvailabilityDto(
    int InventoryItemId,
    int StoreId,
    int OfferId,
    int ProductId,
    int? VariantId,
    bool TrackInventory,
    bool AllowBackorder,
    int OnHand,
    int Reserved,
    int Incoming,
    int Available,
    InventoryAvailabilityStatus AvailabilityStatus,
    bool CanPurchase,
    bool IsBackorder,
    bool IsLowStock);

public sealed record InventoryValidationResult(
    bool IsValid,
    bool IsBackorder,
    IReadOnlyList<string> Messages,
    OfferAvailabilityDto? Availability);

public sealed record InventoryItemSummaryDto(
    int Id,
    int StoreId,
    int OfferId,
    int ProductId,
    int? VariantId,
    int? WarehouseId,
    int? StockLocationId,
    bool TrackInventory,
    bool AllowBackorder,
    int OnHand,
    int Reserved,
    int Incoming,
    int Available,
    int? LowStockThreshold,
    bool IsLowStock,
    InventoryAvailabilityStatus AvailabilityStatus,
    DateTime UpdatedAtUtc);

public sealed record InventoryItemDetailDto(
    int Id,
    int StoreId,
    int OfferId,
    int ProductId,
    int? VariantId,
    int? WarehouseId,
    int? StockLocationId,
    bool TrackInventory,
    bool AllowBackorder,
    int OnHand,
    int Reserved,
    int Incoming,
    int Available,
    int? LowStockThreshold,
    bool IsLowStock,
    InventoryAvailabilityStatus AvailabilityStatus,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record InventoryMovementDto(
    int Id,
    int InventoryItemId,
    int QuantityDelta,
    InventoryMovementType MovementType,
    string Reason,
    InventoryReferenceType ReferenceType,
    int? ReferenceId,
    string? CreatedBy,
    DateTime CreatedAtUtc);

public sealed record InventoryReservationDto(
    int Id,
    int InventoryItemId,
    int Quantity,
    InventoryReferenceType ReferenceType,
    int ReferenceId,
    InventoryReservationStatus Status,
    DateTime ExpiresAtUtc,
    string? ReleaseReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateInventoryItemRequest(
    int OfferId,
    bool TrackInventory,
    bool AllowBackorder,
    int InitialOnHand = 0,
    int? WarehouseId = null,
    int? StockLocationId = null,
    int? LowStockThreshold = null,
    int InitialIncoming = 0);

public sealed record AdjustInventoryStockRequest(
    int QuantityDelta,
    InventoryMovementType MovementType,
    string Reason);

public sealed record InventoryOrderLineDto(
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity);

public sealed record InventoryOrderReservationRequest(
    int OrderId,
    int StoreId,
    IReadOnlyList<InventoryOrderLineDto> Lines);

public sealed record InventoryOrderReservationResult(
    bool Success,
    IReadOnlyList<string> Errors);

public sealed record InventoryListQuery(
    int Page = 1,
    int PageSize = 20,
    int? StoreId = null,
    int? OfferId = null,
    int? ProductId = null,
    int? WarehouseId = null,
    InventoryAvailabilityStatus? AvailabilityStatus = null);

public sealed record PagedInventorySummaryResult(
    IReadOnlyList<InventoryItemSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public interface IInventoryReader
{
    Task<Result<OfferAvailabilityDto>> GetAvailabilityForOfferAsync(
        int offerId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task<InventoryValidationResult> ValidateQuantityAsync(
        int offerId,
        int storeId,
        int quantity,
        CancellationToken cancellationToken = default);
}

public interface IInventoryReservationService
{
    Task<Result<InventoryReservationDto>> ReserveAsync(
        int inventoryItemId,
        int quantity,
        InventoryReferenceType referenceType,
        int referenceId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<Result> ReleaseAsync(
        int reservationId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<Result> ConvertAsync(
        int reservationId,
        CancellationToken cancellationToken = default);
}

public interface IInventoryOrderService
{
    Task<InventoryOrderReservationResult> ReserveForOrderAsync(
        InventoryOrderReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ReleaseForOrderAsync(
        int orderId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task<Result> ConvertForOrderAsync(
        int orderId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task<Result> ReleasePartialForOrderAsync(
        int orderId,
        int storeId,
        IReadOnlyList<InventoryOrderLineAdjustment> lines,
        CancellationToken cancellationToken = default);

    Task<Result> RestockForOrderAsync(
        int orderId,
        int storeId,
        IReadOnlyList<InventoryOrderLineAdjustment> lines,
        string reason,
        CancellationToken cancellationToken = default);
}

public sealed record InventoryOrderLineAdjustment(int OfferId, int Quantity);

public interface IInventoryAdminService
{
    Task<Result<PagedInventorySummaryResult>> ListAsync(
        InventoryListQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<InventoryItemDetailDto>> GetByIdAsync(
        int inventoryItemId,
        CancellationToken cancellationToken = default);

    Task<Result<InventoryItemDetailDto>> CreateAsync(
        CreateInventoryItemRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<InventoryItemDetailDto>> AdjustAsync(
        int inventoryItemId,
        AdjustInventoryStockRequest request,
        string? actor,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<InventoryMovementDto>>> ListMovementsAsync(
        int inventoryItemId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<InventoryReservationDto>>> ListReservationsAsync(
        int inventoryItemId,
        CancellationToken cancellationToken = default);

    Task<Result<InventoryItemDetailDto>> SetLowStockThresholdAsync(
        int inventoryItemId,
        SetLowStockThresholdRequest request,
        CancellationToken cancellationToken = default);
}

public interface IInventoryReservationExpirationService
{
    Task<int> ExpireReservationsAsync(CancellationToken cancellationToken = default);
}

public interface IStorefrontInventoryReader
{
    Task<Result<OfferAvailabilityDto>> GetStorefrontAvailabilityAsync(
        int offerId,
        int storeId,
        CancellationToken cancellationToken = default);
}
