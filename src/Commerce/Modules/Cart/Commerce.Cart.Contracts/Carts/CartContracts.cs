using Commerce.Cart.Contracts.Carts;
using Commerce.Framework.Core.Results;

namespace Commerce.Cart.Contracts.Carts;

public sealed record CartItemDto(
    int Id,
    int OfferId,
    int ProductId,
    int? VariantId,
    string ProductName,
    string? VariantName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal LineSubtotal,
    string Currency,
    bool IsValid,
    IReadOnlyList<string> ValidationMessages,
    CartItemImageDto? PrimaryImage);

public sealed record CartItemImageDto(
    string Url,
    string? ThumbnailUrl,
    string? AltText);

public sealed record CartTotalsDto(
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string Currency);

public sealed record CartDto(
    int Id,
    int StoreId,
    string Currency,
    int CurrencyId,
    IReadOnlyList<CartItemDto> Items,
    CartTotalsDto Totals,
    int ItemCount,
    string? AppliedCouponCode,
    DateTime UpdatedAtUtc);

public sealed record CartMergeConflictDto(
    int OfferId,
    int RequestedQuantity,
    int AppliedQuantity,
    string Reason);

public sealed record CartMergeResultDto(
    CartDto Cart,
    int MergedItemCount,
    IReadOnlyList<CartMergeConflictDto> Conflicts);

public sealed record AddCartItemRequest(int OfferId, int Quantity);

public sealed record UpdateCartItemQuantityRequest(int Quantity);

public sealed record ApplyCartCouponRequest(string Code);

public interface ICartService
{
    Task<Result<CartDto>> GetCartAsync(CancellationToken cancellationToken = default);

    Task<Result<CartDto>> AddItemAsync(AddCartItemRequest request, CancellationToken cancellationToken = default);

    Task<Result<CartDto>> UpdateItemQuantityAsync(
        int cartItemId,
        UpdateCartItemQuantityRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CartDto>> RemoveItemAsync(int cartItemId, CancellationToken cancellationToken = default);

    Task<Result<CartDto>> ClearCartAsync(CancellationToken cancellationToken = default);

    Task<Result<CartDto>> ApplyCouponAsync(ApplyCartCouponRequest request, CancellationToken cancellationToken = default);

    Task<Result<CartDto>> RemoveCouponAsync(string code, CancellationToken cancellationToken = default);

    Task<Result<CartMergeResultDto>> MergeGuestCartAsync(CancellationToken cancellationToken = default);
}

public interface ICartConversionService
{
    Task<Result> MarkConvertedAsync(int cartId, CancellationToken cancellationToken = default);
}
