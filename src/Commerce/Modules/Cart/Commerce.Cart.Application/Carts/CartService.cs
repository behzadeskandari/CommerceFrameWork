using Commerce.Cart.Application.Abstractions;
using Commerce.Cart.Contracts.Carts;
using Commerce.Cart.Domain.Entities;
using Commerce.Cart.Domain.Enums;
using Commerce.Customers.Contracts.Customers;
using Commerce.Framework.Application.Observability;
using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Pricing.Contracts.Pricing;
using Microsoft.Extensions.Logging;

namespace Commerce.Cart.Application.Carts;

public sealed class CartService(
    ICartRepository cartRepository,
    ICartOfferValidator offerValidator,
    ICartTotalsCalculator totalsCalculator,
    ICartItemDisplayEnricher displayEnricher,
    IGuestCartCookieManager guestCartCookieManager,
    ICartGuestTokenGenerator guestTokenGenerator,
    ICurrentCustomerContext currentCustomerContext,
    IStoreContext storeContext,
    IPriceCalculationService priceCalculationService,
    ICouponValidationService couponValidationService,
    CartSettings cartSettings,
    ICorrelationContext correlationContext,
    ILogger<CartService> logger) : ICartService
{
    public Task<Result<CartDto>> GetCartAsync(CancellationToken cancellationToken = default) =>
        BuildCartResponseAsync(resolveOnly: false, cancellationToken);

    public async Task<Result<CartDto>> AddItemAsync(
        AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Quantity <= 0)
        {
            return Result.Failure<CartDto>(CartErrors.InvalidQuantity("Quantity must be greater than zero."));
        }

        var context = await ResolveStoreCurrencyContextAsync(cancellationToken).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return Result.Failure<CartDto>(context.Error!);
        }

        var validation = await offerValidator.ValidateAsync(
            request.OfferId,
            context.Value!.StoreId,
            context.Value.CurrencyId,
            context.Value.CurrencyCode,
            request.Quantity,
            cancellationToken).ConfigureAwait(false);

        if (!validation.IsValid)
        {
            return Result.Failure<CartDto>(
                CartErrors.OfferUnavailable(string.Join(' ', validation.Messages)));
        }

        var cartResult = await GetOrCreateActiveCartAsync(context.Value!, cancellationToken).ConfigureAwait(false);
        if (!cartResult.IsSuccess)
        {
            return Result.Failure<CartDto>(cartResult.Error!);
        }

        var cart = cartResult.Value!;
        var maxItemQuantity = await cartSettings.GetMaxItemQuantityAsync(context.Value.StoreId, cancellationToken).ConfigureAwait(false);
        var maxDistinctItems = await cartSettings.GetMaxDistinctItemsAsync(context.Value.StoreId, cancellationToken).ConfigureAwait(false);

        try
        {
            cart.AddOrIncreaseItem(request.OfferId, request.Quantity, maxItemQuantity, maxDistinctItems);
            await cartRepository.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
            using (CommerceLogging.BeginOperationScope(logger, correlationContext, "cart.item.added", ("CartId", cart.Id), ("OfferId", request.OfferId)))
            {
                CommerceMetrics.CartOperations.Add(1, new KeyValuePair<string, object?>("operation", "item.added"));
                logger.LogInformation("Cart item added for cart {CartId}, offer {OfferId}", cart.Id, request.OfferId);
            }
            return await MapCartAsync(cart, cancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result.Failure<CartDto>(CartErrors.InvalidQuantity(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CartDto>(MapDomainException(ex));
        }
    }

    public async Task<Result<CartDto>> UpdateItemQuantityAsync(
        int cartItemId,
        UpdateCartItemQuantityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Quantity <= 0)
        {
            return Result.Failure<CartDto>(CartErrors.InvalidQuantity("Quantity must be greater than zero."));
        }

        var context = await ResolveStoreCurrencyContextAsync(cancellationToken).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return Result.Failure<CartDto>(context.Error!);
        }

        var cartResult = await ResolveCurrentCartAsync(context.Value!, createIfMissing: false, cancellationToken).ConfigureAwait(false);
        if (!cartResult.IsSuccess)
        {
            return Result.Failure<CartDto>(cartResult.Error!);
        }

        var cart = cartResult.Value!;
        var item = cart.Items.FirstOrDefault(x => x.Id == cartItemId);
        if (item is null)
        {
            return Result.Failure<CartDto>(CartErrors.CartItemNotFound(cartItemId));
        }

        var validation = await offerValidator.ValidateAsync(
            item.OfferId,
            context.Value!.StoreId,
            context.Value.CurrencyId,
            context.Value.CurrencyCode,
            request.Quantity,
            cancellationToken).ConfigureAwait(false);

        if (!validation.IsValid)
        {
            return Result.Failure<CartDto>(
                CartErrors.OfferUnavailable(string.Join(' ', validation.Messages)));
        }

        var maxItemQuantity = await cartSettings.GetMaxItemQuantityAsync(context.Value.StoreId, cancellationToken).ConfigureAwait(false);

        try
        {
            cart.UpdateItemQuantity(cartItemId, request.Quantity, maxItemQuantity);
            await cartRepository.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Cart item quantity updated for cart {CartId}, item {CartItemId}", cart.Id, cartItemId);
            return await MapCartAsync(cart, cancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result.Failure<CartDto>(CartErrors.InvalidQuantity(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CartDto>(MapDomainException(ex));
        }
    }

    public async Task<Result<CartDto>> RemoveItemAsync(int cartItemId, CancellationToken cancellationToken = default)
    {
        var context = await ResolveStoreCurrencyContextAsync(cancellationToken).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return Result.Failure<CartDto>(context.Error!);
        }

        var cartResult = await ResolveCurrentCartAsync(context.Value!, createIfMissing: false, cancellationToken).ConfigureAwait(false);
        if (!cartResult.IsSuccess)
        {
            return Result.Failure<CartDto>(cartResult.Error!);
        }

        var cart = cartResult.Value!;
        if (cart.Items.All(x => x.Id != cartItemId))
        {
            return Result.Failure<CartDto>(CartErrors.CartItemNotFound(cartItemId));
        }

        try
        {
            cart.RemoveItem(cartItemId);
            await cartRepository.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Cart item removed for cart {CartId}, item {CartItemId}", cart.Id, cartItemId);
            return await MapCartAsync(cart, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CartDto>(MapDomainException(ex));
        }
    }

    public async Task<Result<CartDto>> ClearCartAsync(CancellationToken cancellationToken = default)
    {
        var context = await ResolveStoreCurrencyContextAsync(cancellationToken).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return Result.Failure<CartDto>(context.Error!);
        }

        var cartResult = await ResolveCurrentCartAsync(context.Value!, createIfMissing: false, cancellationToken).ConfigureAwait(false);
        if (!cartResult.IsSuccess)
        {
            return Result.Failure<CartDto>(cartResult.Error!);
        }

        var cart = cartResult.Value!;

        try
        {
            cart.ClearItems();
            cart.RemoveCoupon();
            await cartRepository.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Cart cleared for cart {CartId}", cart.Id);
            return await MapCartAsync(cart, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CartDto>(MapDomainException(ex));
        }
    }

    public async Task<Result<CartDto>> ApplyCouponAsync(
        ApplyCartCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = await ResolveStoreCurrencyContextAsync(cancellationToken).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return Result.Failure<CartDto>(context.Error!);
        }

        var cartResult = await ResolveCurrentCartAsync(context.Value!, createIfMissing: false, cancellationToken).ConfigureAwait(false);
        if (!cartResult.IsSuccess)
        {
            return Result.Failure<CartDto>(CartErrors.CartNotFound());
        }

        var cart = cartResult.Value!;
        if (cart.Items.Count == 0)
        {
            return Result.Failure<CartDto>(CartErrors.OfferUnavailable("Cart is empty."));
        }

        var preview = await BuildDiscountPreviewLinesAsync(cart, cancellationToken).ConfigureAwait(false);
        var subtotal = preview.Sum(x => x.LineSubtotal);

        var validation = await couponValidationService.ValidateAsync(
            new CouponValidationRequest(
                request.Code,
                cart.StoreId,
                cart.CurrencyCode,
                cart.CustomerId,
                !cart.CustomerId.HasValue,
                null,
                subtotal,
                DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);

        if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.NormalizedCode))
        {
            return Result.Failure<CartDto>(CartErrors.OfferUnavailable(string.Join(' ', validation.Errors)));
        }

        try
        {
            cart.ApplyCoupon(validation.NormalizedCode);
            await cartRepository.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
            return await MapCartAsync(cart, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CartDto>(MapDomainException(ex));
        }
    }

    public async Task<Result<CartDto>> RemoveCouponAsync(string code, CancellationToken cancellationToken = default)
    {
        var context = await ResolveStoreCurrencyContextAsync(cancellationToken).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return Result.Failure<CartDto>(context.Error!);
        }

        var cartResult = await ResolveCurrentCartAsync(context.Value!, createIfMissing: false, cancellationToken).ConfigureAwait(false);
        if (!cartResult.IsSuccess)
        {
            return Result.Failure<CartDto>(CartErrors.CartNotFound());
        }

        var cart = cartResult.Value!;
        var normalized = code.Trim().ToUpperInvariant();
        if (!string.Equals(cart.AppliedCouponCode, normalized, StringComparison.Ordinal))
        {
            return Result.Failure<CartDto>(CartErrors.OfferUnavailable("Coupon is not applied to this cart."));
        }

        try
        {
            cart.RemoveCoupon();
            await cartRepository.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
            return await MapCartAsync(cart, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CartDto>(MapDomainException(ex));
        }
    }

    public async Task<Result<CartMergeResultDto>> MergeGuestCartAsync(CancellationToken cancellationToken = default)
    {
        if (!currentCustomerContext.IsAuthenticated || currentCustomerContext.CustomerId is not int customerId)
        {
            return Result.Failure<CartMergeResultDto>(CartErrors.CustomerRequired());
        }

        var context = await ResolveStoreCurrencyContextAsync(cancellationToken).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return Result.Failure<CartMergeResultDto>(context.Error!);
        }

        var guestToken = guestCartCookieManager.GetGuestToken();
        if (string.IsNullOrWhiteSpace(guestToken))
        {
            var existing = await ResolveCurrentCartAsync(context.Value!, createIfMissing: true, cancellationToken).ConfigureAwait(false);
            if (!existing.IsSuccess)
            {
                return Result.Failure<CartMergeResultDto>(existing.Error!);
            }

            var dto = await MapCartAsync(existing.Value!, cancellationToken).ConfigureAwait(false);
            return dto.IsSuccess
                ? Result.Success(new CartMergeResultDto(dto.Value!, 0, []))
                : Result.Failure<CartMergeResultDto>(dto.Error!);
        }

        var guestCart = await cartRepository.GetActiveGuestCartAsync(
            context.Value!.StoreId,
            guestToken,
            context.Value.CurrencyId,
            cancellationToken).ConfigureAwait(false);

        var customerCartResult = await GetOrCreateActiveCartAsync(context.Value, cancellationToken).ConfigureAwait(false);
        if (!customerCartResult.IsSuccess)
        {
            return Result.Failure<CartMergeResultDto>(customerCartResult.Error!);
        }

        var customerCart = customerCartResult.Value!;
        var conflicts = new List<CartMergeConflictDto>();
        var mergedCount = 0;

        if (guestCart is not null && guestCart.Id != customerCart.Id && guestCart.Items.Count > 0)
        {
            var maxItemQuantity = await cartSettings.GetMaxItemQuantityAsync(context.Value.StoreId, cancellationToken).ConfigureAwait(false);
            var maxDistinctItems = await cartSettings.GetMaxDistinctItemsAsync(context.Value.StoreId, cancellationToken).ConfigureAwait(false);

            foreach (var guestItem in guestCart.Items.ToList())
            {
                var validation = await offerValidator.ValidateAsync(
                    guestItem.OfferId,
                    context.Value.StoreId,
                    context.Value.CurrencyId,
                    context.Value.CurrencyCode,
                    guestItem.Quantity,
                    cancellationToken).ConfigureAwait(false);

                if (!validation.IsValid)
                {
                    conflicts.Add(new CartMergeConflictDto(
                        guestItem.OfferId,
                        guestItem.Quantity,
                        0,
                        string.Join(' ', validation.Messages)));
                    continue;
                }

                var existingItem = customerCart.Items.FirstOrDefault(x => x.OfferId == guestItem.OfferId);
                var targetQuantity = (existingItem?.Quantity ?? 0) + guestItem.Quantity;
                var appliedQuantity = Math.Min(targetQuantity, maxItemQuantity);

                if (appliedQuantity <= 0)
                {
                    conflicts.Add(new CartMergeConflictDto(
                        guestItem.OfferId,
                        guestItem.Quantity,
                        0,
                        "Quantity exceeds allowed maximum."));
                    continue;
                }

                if (appliedQuantity < targetQuantity)
                {
                    conflicts.Add(new CartMergeConflictDto(
                        guestItem.OfferId,
                        guestItem.Quantity,
                        appliedQuantity - (existingItem?.Quantity ?? 0),
                        $"Quantity capped at {maxItemQuantity}."));
                }

                try
                {
                    if (existingItem is null)
                    {
                        if (customerCart.Items.Count >= maxDistinctItems)
                        {
                            conflicts.Add(new CartMergeConflictDto(
                                guestItem.OfferId,
                                guestItem.Quantity,
                                0,
                                "Cart distinct item limit reached."));
                            continue;
                        }

                        customerCart.AddOrIncreaseItem(guestItem.OfferId, appliedQuantity, maxItemQuantity, maxDistinctItems);
                    }
                    else
                    {
                        customerCart.UpdateItemQuantity(existingItem.Id, appliedQuantity, maxItemQuantity);
                    }

                    mergedCount++;
                }
                catch (Exception ex)
                {
                    conflicts.Add(new CartMergeConflictDto(
                        guestItem.OfferId,
                        guestItem.Quantity,
                        0,
                        ex.Message));
                }
            }

            guestCart.MarkConverted();
            await cartRepository.SaveAsync(guestCart, cancellationToken).ConfigureAwait(false);
            await cartRepository.SaveAsync(customerCart, cancellationToken).ConfigureAwait(false);
            guestCartCookieManager.ClearGuestToken();
            logger.LogInformation(
                "Guest cart merged into customer cart {CartId} for customer {CustomerId}",
                customerCart.Id,
                customerId);
        }
        else
        {
            guestCartCookieManager.ClearGuestToken();
        }

        var cartDto = await MapCartAsync(customerCart, cancellationToken).ConfigureAwait(false);
        return cartDto.IsSuccess
            ? Result.Success(new CartMergeResultDto(cartDto.Value!, mergedCount, conflicts))
            : Result.Failure<CartMergeResultDto>(cartDto.Error!);
    }

    private async Task<Result<CartDto>> BuildCartResponseAsync(bool resolveOnly, CancellationToken cancellationToken)
    {
        var context = await ResolveStoreCurrencyContextAsync(cancellationToken).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return Result.Failure<CartDto>(context.Error!);
        }

        var cartResult = await ResolveCurrentCartAsync(context.Value!, createIfMissing: !resolveOnly, cancellationToken).ConfigureAwait(false);
        if (!cartResult.IsSuccess)
        {
            if (resolveOnly && cartResult.Error!.Type == ErrorType.NotFound)
            {
                return Result.Success(CreateEmptyCartDto(context.Value!));
            }

            return Result.Failure<CartDto>(cartResult.Error!);
        }

        return await MapCartAsync(cartResult.Value!, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<StoreCurrencyContext>> ResolveStoreCurrencyContextAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!storeContext.CurrentStoreId.HasValue)
        {
            return Result.Failure<StoreCurrencyContext>(CartErrors.StoreContextRequired());
        }

        if (!storeContext.CurrentCurrencyId.HasValue || string.IsNullOrWhiteSpace(storeContext.CurrentCurrencyCode))
        {
            return Result.Failure<StoreCurrencyContext>(CartErrors.CurrencyContextRequired());
        }

        return Result.Success(new StoreCurrencyContext(
            storeContext.CurrentStoreId.Value,
            storeContext.CurrentCurrencyId.Value,
            storeContext.CurrentCurrencyCode));
    }

    private async Task<Result<ShoppingCart>> GetOrCreateActiveCartAsync(
        StoreCurrencyContext context,
        CancellationToken cancellationToken)
    {
        var result = await ResolveCurrentCartAsync(context, createIfMissing: true, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task<Result<ShoppingCart>> ResolveCurrentCartAsync(
        StoreCurrencyContext context,
        bool createIfMissing,
        CancellationToken cancellationToken)
    {
        if (currentCustomerContext.IsAuthenticated && currentCustomerContext.CustomerId is int customerId)
        {
            var cart = await cartRepository.GetActiveCustomerCartAsync(
                context.StoreId,
                customerId,
                context.CurrencyId,
                cancellationToken).ConfigureAwait(false);

            if (cart is not null)
            {
                return EnsureCartUsable(cart);
            }

            if (!createIfMissing)
            {
                return Result.Failure<ShoppingCart>(CartErrors.CartNotFound());
            }

            var expiresAt = DateTime.UtcNow.AddDays(
                await cartSettings.GetCustomerExpirationDaysAsync(context.StoreId, cancellationToken).ConfigureAwait(false));
            var created = ShoppingCart.CreateForCustomer(
                context.StoreId,
                customerId,
                context.CurrencyId,
                context.CurrencyCode,
                expiresAt);
            await cartRepository.AddAsync(created, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Customer cart created {CartId} for customer {CustomerId}", created.Id, customerId);
            return Result.Success(created);
        }

        var guestToken = guestCartCookieManager.GetGuestToken();
        if (!string.IsNullOrWhiteSpace(guestToken))
        {
            var guestCart = await cartRepository.GetActiveGuestCartAsync(
                context.StoreId,
                guestToken,
                context.CurrencyId,
                cancellationToken).ConfigureAwait(false);

            if (guestCart is not null)
            {
                return EnsureCartUsable(guestCart);
            }
        }

        if (!createIfMissing)
        {
            return Result.Failure<ShoppingCart>(CartErrors.CartNotFound());
        }

        var newToken = guestTokenGenerator.GenerateToken();
        var guestExpiresAt = DateTime.UtcNow.AddHours(
            await cartSettings.GetGuestExpirationHoursAsync(context.StoreId, cancellationToken).ConfigureAwait(false));
        var guestCartCreated = ShoppingCart.CreateForGuest(
            context.StoreId,
            newToken,
            context.CurrencyId,
            context.CurrencyCode,
            guestExpiresAt);
        await cartRepository.AddAsync(guestCartCreated, cancellationToken).ConfigureAwait(false);
        guestCartCookieManager.SetGuestToken(newToken, guestExpiresAt);
        logger.LogInformation("Guest cart created {CartId}", guestCartCreated.Id);
        return Result.Success(guestCartCreated);
    }

    private static Result<ShoppingCart> EnsureCartUsable(ShoppingCart cart)
    {
        if (cart.Status is CartStatus.Converted)
        {
            return Result.Failure<ShoppingCart>(CartErrors.CartConverted());
        }

        if (cart.Status is CartStatus.Expired || DateTime.UtcNow >= cart.ExpiresAtUtc)
        {
            return Result.Failure<ShoppingCart>(CartErrors.CartExpired());
        }

        return Result.Success(cart);
    }

    private async Task<Result<CartDto>> MapCartAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        var context = await ResolveStoreCurrencyContextAsync(cancellationToken).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return Result.Failure<CartDto>(context.Error!);
        }

        var lineTotals = new List<CartLineTotals>();
        var itemDtos = new List<CartItemDto>();
        var displayKeys = new List<(int OfferId, int ProductId, int? VariantId)>();

        foreach (var item in cart.Items)
        {
            var validation = await offerValidator.ValidateAsync(
                item.OfferId,
                cart.StoreId,
                cart.CurrencyId,
                cart.CurrencyCode,
                item.Quantity,
                cancellationToken).ConfigureAwait(false);

            displayKeys.Add((item.OfferId, validation.ProductId, validation.VariantId));

            var line = totalsCalculator.CalculateLine(validation.UnitPrice, item.Quantity, validation.CurrencyCode);
            if (validation.IsValid)
            {
                lineTotals.Add(line);
            }

            itemDtos.Add(new CartItemDto(
                item.Id,
                item.OfferId,
                validation.ProductId,
                validation.VariantId,
                validation.ProductName,
                validation.VariantName,
                validation.Sku,
                item.Quantity,
                validation.UnitPrice,
                validation.IsValid ? line.LineSubtotal : 0m,
                validation.CurrencyCode,
                validation.IsValid,
                validation.Messages,
                null));
        }

        var images = await displayEnricher.GetPrimaryImagesByOfferAsync(displayKeys, cancellationToken).ConfigureAwait(false);
        itemDtos = itemDtos
            .Select(dto => dto with
            {
                PrimaryImage = images.GetValueOrDefault(dto.OfferId) is { } image
                    ? new CartItemImageDto(image.Url, image.ThumbnailUrl, image.AltText)
                    : null
            })
            .ToList();

        var aggregate = await CalculateCartTotalsAsync(cart, lineTotals, itemDtos, cancellationToken).ConfigureAwait(false);
        var totals = new CartTotalsDto(
            aggregate.Subtotal,
            aggregate.DiscountTotal,
            aggregate.ShippingTotal,
            aggregate.TaxTotal,
            aggregate.GrandTotal,
            aggregate.CurrencyCode);

        return Result.Success(new CartDto(
            cart.Id,
            cart.StoreId,
            cart.CurrencyCode,
            cart.CurrencyId,
            itemDtos,
            totals,
            itemDtos.Sum(x => x.Quantity),
            cart.AppliedCouponCode,
            cart.UpdatedAtUtc));
    }

    private async Task<CartAggregateTotals> CalculateCartTotalsAsync(
        ShoppingCart cart,
        IReadOnlyList<CartLineTotals> lineTotals,
        IReadOnlyList<CartItemDto> itemDtos,
        CancellationToken cancellationToken)
    {
        if (lineTotals.Count == 0)
        {
            return totalsCalculator.CalculateCart(lineTotals, cart.CurrencyCode);
        }

        var discountContext = new CartDiscountCalculationContext(
            cart.StoreId,
            cart.Id,
            cart.CurrencyCode,
            cart.CustomerId,
            !cart.CustomerId.HasValue,
            CustomerGroupId: null,
            itemDtos.Where(x => x.IsValid).Select(x => new CartDiscountLineContext(
                x.OfferId,
                x.ProductId,
                x.VariantId,
                x.Quantity,
                x.UnitPrice)).ToList(),
            cart.AppliedCouponCode,
            DateTime.UtcNow);

        var discountResult = await priceCalculationService
            .CalculateCartAsync(discountContext, cancellationToken)
            .ConfigureAwait(false);

        return totalsCalculator.CalculateFromDiscountResult(discountResult);
    }

    private async Task<IReadOnlyList<CartLineTotals>> BuildDiscountPreviewLinesAsync(
        ShoppingCart cart,
        CancellationToken cancellationToken)
    {
        var lines = new List<CartLineTotals>();
        foreach (var item in cart.Items)
        {
            var validation = await offerValidator.ValidateAsync(
                item.OfferId,
                cart.StoreId,
                cart.CurrencyId,
                cart.CurrencyCode,
                item.Quantity,
                cancellationToken).ConfigureAwait(false);

            if (validation.IsValid)
            {
                lines.Add(totalsCalculator.CalculateLine(validation.UnitPrice, item.Quantity, validation.CurrencyCode));
            }
        }

        return lines;
    }

    private static CartDto CreateEmptyCartDto(StoreCurrencyContext context) =>
        new(
            Id: 0,
            context.StoreId,
            context.CurrencyCode,
            context.CurrencyId,
            [],
            new CartTotalsDto(0m, 0m, 0m, 0m, 0m, context.CurrencyCode),
            0,
            null,
            DateTime.UtcNow);

    private static Error MapDomainException(InvalidOperationException ex) =>
        ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase)
            ? CartErrors.CartExpired()
            : ex.Message.Contains("converted", StringComparison.OrdinalIgnoreCase)
                ? CartErrors.CartConverted()
                : Error.Validation(ex.Message);

    private sealed record StoreCurrencyContext(int StoreId, int CurrencyId, string CurrencyCode);
}
