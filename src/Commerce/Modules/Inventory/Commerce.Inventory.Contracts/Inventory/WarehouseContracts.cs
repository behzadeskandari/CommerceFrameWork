using Commerce.Framework.Core.Results;
using Commerce.Inventory.Domain.Enums;

namespace Commerce.Inventory.Contracts.Inventory;

public sealed record WarehouseSummaryDto(
    int Id,
    int StoreId,
    string Name,
    string SystemName,
    bool IsDefault,
    bool IsActive,
    int DisplayOrder);

public sealed record WarehouseDetailDto(
    int Id,
    int StoreId,
    string Name,
    string SystemName,
    bool IsDefault,
    bool IsActive,
    int DisplayOrder,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<StockLocationSummaryDto> Locations);

public sealed record StockLocationSummaryDto(
    int Id,
    int WarehouseId,
    string Code,
    string Name,
    bool IsDefault,
    bool IsActive);

public sealed record CreateWarehouseRequest(
    string Name,
    string SystemName,
    bool IsDefault,
    int DisplayOrder = 0);

public sealed record UpdateWarehouseRequest(
    string Name,
    int DisplayOrder);

public sealed record CreateStockLocationRequest(
    string Code,
    string Name,
    bool IsDefault);

public sealed record TransferInventoryStockRequest(
    int SourceInventoryItemId,
    int DestinationInventoryItemId,
    int Quantity,
    string Reason);

public sealed record ReceiveIncomingStockRequest(
    int Quantity,
    string Reason);

public sealed record SetLowStockThresholdRequest(int? Threshold);

public interface IWarehouseAdminService
{
    Task<Result<IReadOnlyList<WarehouseSummaryDto>>> ListWarehousesAsync(
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<Result<WarehouseDetailDto>> GetWarehouseAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<WarehouseDetailDto>> CreateWarehouseAsync(
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<WarehouseDetailDto>> UpdateWarehouseAsync(
        int id,
        UpdateWarehouseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ActivateWarehouseAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> DeactivateWarehouseAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<StockLocationSummaryDto>> CreateStockLocationAsync(
        int warehouseId,
        CreateStockLocationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IInventoryTransferService
{
    Task<Result<(InventoryMovementDto SourceMovement, InventoryMovementDto DestinationMovement)>> TransferAsync(
        TransferInventoryStockRequest request,
        string? actor,
        CancellationToken cancellationToken = default);

    Task<Result<InventoryItemDetailDto>> ReceiveIncomingAsync(
        int inventoryItemId,
        ReceiveIncomingStockRequest request,
        string? actor,
        CancellationToken cancellationToken = default);
}
