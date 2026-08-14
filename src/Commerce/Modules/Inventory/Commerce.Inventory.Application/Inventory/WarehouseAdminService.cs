using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Results;
using Commerce.Inventory.Application.Abstractions;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Inventory.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Commerce.Inventory.Application.Inventory;

public sealed class WarehouseAdminService(
    IInventoryRepository inventoryRepository,
    IStoreContext storeContext,
    ILogger<WarehouseAdminService> logger) : IWarehouseAdminService
{
    public async Task<Result<IReadOnlyList<WarehouseSummaryDto>>> ListWarehousesAsync(
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var resolvedStoreId = storeId ?? storeContext.CurrentStoreId;
        if (!resolvedStoreId.HasValue)
        {
            return Result.Failure<IReadOnlyList<WarehouseSummaryDto>>(InventoryErrors.StoreMismatch());
        }

        var items = await inventoryRepository.ListWarehousesAsync(resolvedStoreId.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<WarehouseSummaryDto>>(items.Select(InventoryMapper.ToWarehouseSummary).ToList());
    }

    public async Task<Result<WarehouseDetailDto>> GetWarehouseAsync(int id, CancellationToken cancellationToken = default)
    {
        var warehouse = await inventoryRepository.GetWarehouseByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (warehouse is null)
        {
            return Result.Failure<WarehouseDetailDto>(InventoryErrors.WarehouseNotFound(id));
        }

        var locations = await inventoryRepository.ListStockLocationsAsync(id, cancellationToken).ConfigureAwait(false);
        return Result.Success(new WarehouseDetailDto(
            warehouse.Id,
            warehouse.StoreId,
            warehouse.Name,
            warehouse.SystemName,
            warehouse.IsDefault,
            warehouse.IsActive,
            warehouse.DisplayOrder,
            warehouse.CreatedAtUtc,
            warehouse.UpdatedAtUtc,
            locations.Select(InventoryMapper.ToStockLocationSummary).ToList()));
    }

    public async Task<Result<WarehouseDetailDto>> CreateWarehouseAsync(
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        var storeId = storeContext.CurrentStoreId;
        if (!storeId.HasValue)
        {
            return Result.Failure<WarehouseDetailDto>(InventoryErrors.StoreMismatch());
        }

        var existing = (await inventoryRepository.ListWarehousesAsync(storeId.Value, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(x => x.SystemName == request.SystemName.Trim().ToLowerInvariant());
        if (existing is not null)
        {
            return Result.Failure<WarehouseDetailDto>(InventoryErrors.WarehouseAlreadyExists(request.SystemName));
        }

        var warehouse = Warehouse.Create(storeId.Value, request.Name, request.SystemName, request.IsDefault, request.DisplayOrder);
        if (request.IsDefault)
        {
            await inventoryRepository.ClearDefaultWarehouseAsync(storeId.Value, 0, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var warehouses = await inventoryRepository.ListWarehousesAsync(storeId.Value, cancellationToken).ConfigureAwait(false);
            if (warehouses.Count == 0)
            {
                warehouse.SetDefault(true);
            }
        }

        await inventoryRepository.AddWarehouseAsync(warehouse, cancellationToken).ConfigureAwait(false);
        var defaultLocation = StockLocation.Create(warehouse.Id, "DEFAULT", "Default location", isDefault: true);
        await inventoryRepository.AddStockLocationAsync(defaultLocation, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Warehouse {SystemName} created for store {StoreId}.", warehouse.SystemName, storeId.Value);
        return await GetWarehouseAsync(warehouse.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<WarehouseDetailDto>> UpdateWarehouseAsync(
        int id,
        UpdateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await inventoryRepository.GetWarehouseByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (warehouse is null)
        {
            return Result.Failure<WarehouseDetailDto>(InventoryErrors.WarehouseNotFound(id));
        }

        warehouse.Update(request.Name, request.DisplayOrder);
        await inventoryRepository.SaveWarehouseAsync(warehouse, cancellationToken).ConfigureAwait(false);
        return await GetWarehouseAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> ActivateWarehouseAsync(int id, CancellationToken cancellationToken = default)
    {
        var warehouse = await inventoryRepository.GetWarehouseByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (warehouse is null)
        {
            return Result.Failure(InventoryErrors.WarehouseNotFound(id));
        }

        warehouse.Activate();
        await inventoryRepository.SaveWarehouseAsync(warehouse, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DeactivateWarehouseAsync(int id, CancellationToken cancellationToken = default)
    {
        var warehouse = await inventoryRepository.GetWarehouseByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (warehouse is null)
        {
            return Result.Failure(InventoryErrors.WarehouseNotFound(id));
        }

        warehouse.Deactivate();
        await inventoryRepository.SaveWarehouseAsync(warehouse, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<StockLocationSummaryDto>> CreateStockLocationAsync(
        int warehouseId,
        CreateStockLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await inventoryRepository.GetWarehouseByIdAsync(warehouseId, cancellationToken).ConfigureAwait(false);
        if (warehouse is null)
        {
            return Result.Failure<StockLocationSummaryDto>(InventoryErrors.WarehouseNotFound(warehouseId));
        }

        var location = StockLocation.Create(warehouseId, request.Code, request.Name, request.IsDefault);
        await inventoryRepository.AddStockLocationAsync(location, cancellationToken).ConfigureAwait(false);
        return Result.Success(InventoryMapper.ToStockLocationSummary(location));
    }
}
