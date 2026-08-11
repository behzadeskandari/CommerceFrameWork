using Commerce.Cart.Contracts.Carts;
using Commerce.Checkout.Application.Abstractions;
using Commerce.Checkout.Contracts.Checkout;
using Commerce.Checkout.Domain.Entities;
using Commerce.Checkout.Domain.Enums;
using Commerce.Checkout.Domain.ValueObjects;
using Commerce.Customers.Contracts.Customers;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Commerce.Checkout.Application.Checkout;

public sealed class CheckoutService(
    ICheckoutRepository checkoutRepository,
    ICartService cartService,
    IGuestCartContext guestCartContext,
    ICurrentCustomerContext currentCustomerContext,
    ICustomerReader customerReader,
    ICustomerAddressReader customerAddressReader,
    ICheckoutOfferValidator offerValidator,
    ICheckoutItemEnricher itemEnricher,
    CheckoutRequiresShippingEvaluator requiresShippingEvaluator,
    ICheckoutTotalsCalculator totalsCalculator,
    IEnumerable<IShippingRateProvider> shippingProviders,
    IEnumerable<IPaymentMethodProvider> paymentProviders,
    IStoreContext storeContext,
    CheckoutSettings checkoutSettings,
    ILogger<CheckoutService> logger) : ICheckoutService, ICheckoutOrderPreparationService
{
    public async Task<Result<CheckoutDto>> StartAsync(CancellationToken cancellationToken = default)
    {
        var context = ResolveContext();
        if (!context.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(context.Error!);
        }

        var cartResult = await cartService.GetCartAsync(cancellationToken).ConfigureAwait(false);
        if (!cartResult.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(cartResult.Error!);
        }

        var cart = cartResult.Value!;
        if (cart.ItemCount <= 0 || cart.Items.Count == 0)
        {
            return Result.Failure<CheckoutDto>(CheckoutErrors.CheckoutCartEmpty());
        }

        if (cart.Items.Any(x => !x.IsValid))
        {
            return Result.Failure<CheckoutDto>(
                CheckoutErrors.CartInvalid("One or more cart items are invalid."));
        }

        var existing = await checkoutRepository.GetActiveByCartIdAsync(cart.Id, cancellationToken).ConfigureAwait(false);
        if (existing is not null && existing.IsOwnedBy(context.Value!.StoreId, context.Value.CustomerId, context.Value.GuestToken))
        {
            if (existing.Status is not CheckoutStatus.Expired and not CheckoutStatus.Cancelled and not CheckoutStatus.Completed)
            {
                var refreshed = await RefreshSessionAsync(existing, cart, cancellationToken).ConfigureAwait(false);
                return refreshed.IsSuccess
                    ? Result.Success(await MapAsync(refreshed.Value!, cancellationToken).ConfigureAwait(false))
                    : Result.Failure<CheckoutDto>(refreshed.Error!);
            }
        }

        var requiresShipping = await requiresShippingEvaluator
            .RequiresShippingAsync(cart.Items.Select(x => x.ProductId).ToList(), cancellationToken)
            .ConfigureAwait(false);

        var expiresAt = DateTime.UtcNow.AddMinutes(
            await checkoutSettings.GetExpirationMinutesAsync(context.Value!.StoreId, cancellationToken).ConfigureAwait(false));

        var buildItemsResult = await BuildValidatedItemsAsync(
            0,
            cart,
            context.Value!,
            cancellationToken).ConfigureAwait(false);
        if (!buildItemsResult.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(buildItemsResult.Error!);
        }

        var priceChangeDetected = buildItemsResult.Value!.Any(x => x.UnitPrice != x.PreviousUnitPrice);
        var session = CheckoutSession.Create(
            context.Value.StoreId,
            cart.Id,
            context.Value.CustomerId,
            context.Value.GuestToken,
            cart.CurrencyId,
            cart.Currency,
            requiresShipping,
            cart.UpdatedAtUtc,
            expiresAt,
            buildItemsResult.Value!);

        if (priceChangeDetected)
        {
            session.MarkRequiresReview();
        }

        if (context.Value.CustomerId is int customerId)
        {
            var customer = await customerReader.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
            if (customer.IsSuccess && customer.Value is not null)
            {
                var defaultBilling = customer.Value.Addresses.FirstOrDefault(x => x.IsDefaultBilling)
                    ?? customer.Value.Addresses.FirstOrDefault();
                var defaultShipping = customer.Value.Addresses.FirstOrDefault(x => x.IsDefaultShipping)
                    ?? customer.Value.Addresses.FirstOrDefault();

                if (defaultBilling is not null)
                {
                    session.SetBillingAddress(MapAddressSnapshot(defaultBilling), useShippingAsBilling: false);
                }

                if (requiresShipping && defaultShipping is not null)
                {
                    session.SetShippingAddress(MapAddressSnapshot(defaultShipping));
                }
            }
        }

        await checkoutRepository.AddAsync(session, cancellationToken).ConfigureAwait(false);
        var initialDto = await MapAsync(session, cancellationToken).ConfigureAwait(false);
        await ApplyTotalsAsync(session, initialDto, cancellationToken).ConfigureAwait(false);
        await checkoutRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Checkout started {CheckoutId} for cart {CartId}", session.Id, cart.Id);
        return Result.Success(await MapAsync(session, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<CheckoutDto>> GetAsync(int checkoutId, CancellationToken cancellationToken = default)
    {
        var sessionResult = await LoadOwnedSessionAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        return sessionResult.IsSuccess
            ? Result.Success(await MapAsync(sessionResult.Value!, cancellationToken).ConfigureAwait(false))
            : Result.Failure<CheckoutDto>(sessionResult.Error!);
    }

    public async Task<Result<CheckoutDto>> SetGuestContactAsync(
        int checkoutId,
        SetGuestContactRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sessionResult = await LoadModifiableSessionAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(sessionResult.Error!);
        }

        try
        {
            sessionResult.Value!.SetGuestEmail(request.Email);
            await checkoutRepository.SaveAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false);
            return Result.Success(await MapAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CheckoutDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<CheckoutDto>> SetBillingAddressAsync(
        int checkoutId,
        SetBillingAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sessionResult = await LoadModifiableSessionAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(sessionResult.Error!);
        }

        var addressResult = await ResolveAddressAsync(sessionResult.Value!, request.CustomerAddressId, request.Address, cancellationToken).ConfigureAwait(false);
        if (!addressResult.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(addressResult.Error!);
        }

        sessionResult.Value!.SetBillingAddress(addressResult.Value!, request.UseShippingAsBilling);
        await checkoutRepository.SaveAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Billing address updated for checkout {CheckoutId}", checkoutId);
        return Result.Success(await MapAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<CheckoutDto>> SetShippingAddressAsync(
        int checkoutId,
        SetShippingAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sessionResult = await LoadModifiableSessionAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(sessionResult.Error!);
        }

        if (!sessionResult.Value!.RequiresShipping)
        {
            return Result.Success(await MapAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false));
        }

        var addressResult = await ResolveAddressAsync(sessionResult.Value, request.CustomerAddressId, request.Address, cancellationToken).ConfigureAwait(false);
        if (!addressResult.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(addressResult.Error!);
        }

        sessionResult.Value.SetShippingAddress(addressResult.Value!);
        await checkoutRepository.SaveAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Shipping address updated for checkout {CheckoutId}", checkoutId);
        return Result.Success(await MapAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<CheckoutDto>> SelectShippingMethodAsync(
        int checkoutId,
        SelectShippingMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sessionResult = await LoadModifiableSessionAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(sessionResult.Error!);
        }

        var dto = await MapAsync(sessionResult.Value!, cancellationToken).ConfigureAwait(false);
        var option = dto.ShippingOptions.FirstOrDefault(x =>
            x.Id == request.MethodId &&
            x.ProviderSystemName == request.ProviderSystemName);

        if (option is null)
        {
            return Result.Failure<CheckoutDto>(CheckoutErrors.ShippingMethodNotFound());
        }

        sessionResult.Value!.SelectShippingMethod(option.Id, option.ProviderSystemName, option.Price);
        await ApplyTotalsAsync(sessionResult.Value, dto, cancellationToken).ConfigureAwait(false);
        await checkoutRepository.SaveAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(await MapAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<CheckoutDto>> SelectPaymentMethodAsync(
        int checkoutId,
        SelectPaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sessionResult = await LoadModifiableSessionAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(sessionResult.Error!);
        }

        var dto = await MapAsync(sessionResult.Value!, cancellationToken).ConfigureAwait(false);
        var method = dto.PaymentMethods.FirstOrDefault(x =>
            x.Id == request.MethodId &&
            x.SystemName == request.SystemName);

        if (method is null)
        {
            return Result.Failure<CheckoutDto>(CheckoutErrors.PaymentMethodNotFound());
        }

        sessionResult.Value!.SelectPaymentMethod(method.Id, method.SystemName);
        await checkoutRepository.SaveAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(await MapAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<CheckoutDto>> RefreshAsync(int checkoutId, CancellationToken cancellationToken = default)
    {
        var sessionResult = await LoadOwnedSessionAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(sessionResult.Error!);
        }

        var cartResult = await cartService.GetCartAsync(cancellationToken).ConfigureAwait(false);
        if (!cartResult.IsSuccess)
        {
            return Result.Failure<CheckoutDto>(cartResult.Error!);
        }

        var refreshed = await RefreshSessionAsync(sessionResult.Value!, cartResult.Value!, cancellationToken).ConfigureAwait(false);
        return refreshed.IsSuccess
            ? Result.Success(await MapAsync(refreshed.Value!, cancellationToken).ConfigureAwait(false))
            : Result.Failure<CheckoutDto>(refreshed.Error!);
    }

    public async Task<Result<CheckoutValidationResultDto>> ValidateAsync(
        int checkoutId,
        CancellationToken cancellationToken = default)
    {
        var refresh = await RefreshAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (!refresh.IsSuccess)
        {
            return Result.Failure<CheckoutValidationResultDto>(refresh.Error!);
        }

        var sessionResult = await LoadOwnedSessionAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<CheckoutValidationResultDto>(sessionResult.Error!);
        }

        var checkout = refresh.Value!;
        var errors = checkout.ValidationErrors.ToList();
        var warnings = checkout.Warnings.ToList();

        if (errors.Count == 0 && checkout.Status is not CheckoutStatus.ReadyForOrder)
        {
            sessionResult.Value!.MarkReadyForOrder();
            await checkoutRepository.SaveAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false);
            checkout = await MapAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Checkout validated and ready for order {CheckoutId}", checkoutId);
        }

        return Result.Success(new CheckoutValidationResultDto(
            checkout,
            errors.Count == 0,
            checkout.Status == CheckoutStatus.ReadyForOrder,
            errors,
            warnings));
    }

    public Task<Result<OrderPreparationResult>> ValidateForOrderCreationAsync(
        int checkoutId,
        CancellationToken cancellationToken = default) =>
        PrepareOrderAsync(checkoutId, cancellationToken);

    private async Task<Result<OrderPreparationResult>> PrepareOrderAsync(
        int checkoutId,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (!validation.IsSuccess)
        {
            return Result.Failure<OrderPreparationResult>(validation.Error!);
        }

        if (!validation.Value!.IsReadyForOrder)
        {
            return Result.Failure<OrderPreparationResult>(
                Error.Validation("Checkout is not ready for order creation."));
        }

        var checkout = validation.Value.Checkout;
        return Result.Success(new OrderPreparationResult(
            checkout.Id,
            checkout.StoreId,
            checkout.CartId,
            checkout.Customer.CustomerId,
            checkout.Currency,
            checkout.CurrencyId,
            checkout.Customer.Email,
            checkout.BillingAddress,
            checkout.ShippingAddress,
            checkout.RequiresShipping,
            checkout.SelectedShippingMethodId,
            checkout.ShippingOptions.FirstOrDefault(x => x.Id == checkout.SelectedShippingMethodId)?.ProviderSystemName,
            checkout.Totals.ShippingTotal,
            checkout.SelectedPaymentMethodId,
            checkout.PaymentMethods.FirstOrDefault(x => x.Id == checkout.SelectedPaymentMethodId)?.SystemName,
            checkout.Items.Select(x => new OrderPreparationLineDto(
                x.CartItemId,
                x.OfferId,
                x.ProductId,
                x.VariantId,
                x.ProductName,
                x.VariantName,
                x.Sku,
                x.Quantity,
                x.UnitPrice,
                x.LineSubtotal,
                x.Currency,
                x.PrimaryImage?.Url,
                x.PrimaryImage?.ThumbnailUrl)).ToList(),
            checkout.Totals));
    }

    private async Task<Result<CheckoutSession>> RefreshSessionAsync(
        CheckoutSession session,
        CartDto cart,
        CancellationToken cancellationToken)
    {
        if (cart.Id != session.CartId)
        {
            return Result.Failure<CheckoutSession>(CheckoutErrors.CartInvalid("Cart no longer matches checkout."));
        }

        if (cart.ItemCount <= 0)
        {
            return Result.Failure<CheckoutSession>(CheckoutErrors.CheckoutCartEmpty());
        }

        var context = ResolveContext();
        if (!context.IsSuccess)
        {
            return Result.Failure<CheckoutSession>(context.Error!);
        }

        var priceChangeDetected = false;
        var items = new List<CheckoutSessionItem>();
        foreach (var cartItem in cart.Items)
        {
            var existing = session.Items.FirstOrDefault(x => x.CartItemId == cartItem.Id);
            var validation = await offerValidator.ValidateLineAsync(
                cartItem.OfferId,
                cartItem.Quantity,
                session.StoreId,
                session.CurrencyId,
                session.CurrencyCode,
                existing?.UnitPrice ?? cartItem.UnitPrice,
                cancellationToken).ConfigureAwait(false);

            if (!validation.IsValid)
            {
                return Result.Failure<CheckoutSession>(
                    CheckoutErrors.CartInvalid(string.Join(' ', validation.Messages)));
            }

            if (validation.UnitPrice != validation.PreviousUnitPrice)
            {
                priceChangeDetected = true;
            }

            var lineSubtotal = validation.UnitPrice * cartItem.Quantity;
            items.Add(CheckoutSessionItem.Create(
                session.Id,
                cartItem.Id,
                cartItem.OfferId,
                validation.ProductId,
                validation.VariantId,
                cartItem.Quantity,
                validation.UnitPrice,
                lineSubtotal,
                validation.CurrencyCode,
                validation.PreviousUnitPrice));
        }

        session.ReplaceItems(items, cart.UpdatedAtUtc, priceChangeDetected);
        var dto = await MapAsync(session, cancellationToken).ConfigureAwait(false);
        await ApplyTotalsAsync(session, dto, cancellationToken).ConfigureAwait(false);
        await checkoutRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Checkout refreshed {CheckoutId}", session.Id);
        return Result.Success(session);
    }

    private async Task ApplyTotalsAsync(
        CheckoutSession session,
        CheckoutDto dto,
        CancellationToken cancellationToken)
    {
        var totals = await totalsCalculator.CalculateAsync(
            new CheckoutTotalContext(
                session.StoreId,
                session.CurrencyCode,
                session.CustomerId,
                session.Subtotal,
                session.ShippingTotal,
                dto.BillingAddress,
                dto.ShippingAddress,
                dto.Items.Select(x => new ShippingRateLineItem(
                    x.OfferId,
                    x.ProductId,
                    x.VariantId,
                    x.Quantity,
                    x.UnitPrice,
                    string.Empty)).ToList(),
                []),
            cancellationToken).ConfigureAwait(false);

        session.ApplyTotals(totals.DiscountTotal, totals.ShippingTotal, totals.TaxTotal);
    }

    private async Task<Result<List<CheckoutSessionItem>>> BuildValidatedItemsAsync(
        int checkoutSessionId,
        CartDto cart,
        CheckoutOwnershipContext ownership,
        CancellationToken cancellationToken)
    {
        _ = ownership;
        var items = new List<CheckoutSessionItem>();
        foreach (var cartItem in cart.Items)
        {
            var validation = await offerValidator.ValidateLineAsync(
                cartItem.OfferId,
                cartItem.Quantity,
                ownership.StoreId,
                cart.CurrencyId,
                cart.Currency,
                cartItem.UnitPrice,
                cancellationToken).ConfigureAwait(false);

            if (!validation.IsValid)
            {
                return Result.Failure<List<CheckoutSessionItem>>(
                    CheckoutErrors.CartInvalid(string.Join(' ', validation.Messages)));
            }

            items.Add(CheckoutSessionItem.Create(
                checkoutSessionId,
                cartItem.Id,
                cartItem.OfferId,
                validation.ProductId,
                validation.VariantId,
                cartItem.Quantity,
                validation.UnitPrice,
                validation.UnitPrice * cartItem.Quantity,
                validation.CurrencyCode,
                cartItem.UnitPrice));
        }

        return Result.Success(items);
    }

    private async Task<Result<CheckoutAddressSnapshot>> ResolveAddressAsync(
        CheckoutSession session,
        int? customerAddressId,
        CheckoutAddressRequest? addressRequest,
        CancellationToken cancellationToken)
    {
        if (customerAddressId.HasValue)
        {
            if (!session.CustomerId.HasValue)
            {
                return Result.Failure<CheckoutAddressSnapshot>(CheckoutErrors.UnauthorizedCheckoutAccess());
            }

            var saved = await customerAddressReader
                .GetByIdAsync(session.CustomerId.Value, customerAddressId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!saved.IsSuccess || saved.Value is null)
            {
                return Result.Failure<CheckoutAddressSnapshot>(CheckoutErrors.AddressNotFound());
            }

            return Result.Success(MapAddressSnapshot(saved.Value));
        }

        if (addressRequest is null)
        {
            return Result.Failure<CheckoutAddressSnapshot>(Error.Validation("Address is required."));
        }

        try
        {
            var address = Address.Create(
                addressRequest.FirstName,
                addressRequest.LastName,
                addressRequest.Country,
                addressRequest.City,
                addressRequest.Address1,
                addressRequest.PostalCode,
                addressRequest.StateProvince,
                addressRequest.Address2,
                addressRequest.PhoneNumber);
            return Result.Success(CheckoutAddressSnapshot.FromAddress(address));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CheckoutAddressSnapshot>(Error.Validation(ex.Message));
        }
    }

    private async Task<Result<CheckoutSession>> LoadOwnedSessionAsync(
        int checkoutId,
        CancellationToken cancellationToken)
    {
        var context = ResolveContext();
        if (!context.IsSuccess)
        {
            return Result.Failure<CheckoutSession>(context.Error!);
        }

        var session = await checkoutRepository.GetByIdWithItemsAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return Result.Failure<CheckoutSession>(CheckoutErrors.CheckoutNotFound());
        }

        if (!session.IsOwnedBy(context.Value!.StoreId, context.Value.CustomerId, context.Value.GuestToken))
        {
            return Result.Failure<CheckoutSession>(CheckoutErrors.UnauthorizedCheckoutAccess());
        }

        if (DateTime.UtcNow >= session.ExpiresAtUtc && session.Status is not CheckoutStatus.Expired)
        {
            session.MarkExpired();
            await checkoutRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        }

        if (session.Status is CheckoutStatus.Expired)
        {
            return Result.Failure<CheckoutSession>(CheckoutErrors.CheckoutExpired());
        }

        var cartResult = await cartService.GetCartAsync(cancellationToken).ConfigureAwait(false);
        if (cartResult.IsSuccess &&
            cartResult.Value is not null &&
            cartResult.Value.Id == session.CartId &&
            cartResult.Value.UpdatedAtUtc > session.CartUpdatedAtUtc)
        {
            session.MarkCartStale(cartResult.Value.UpdatedAtUtc);
            await checkoutRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Checkout requires review {CheckoutId} due to cart changes", checkoutId);
        }

        return Result.Success(session);
    }

    private async Task<Result<CheckoutSession>> LoadModifiableSessionAsync(
        int checkoutId,
        CancellationToken cancellationToken)
    {
        var sessionResult = await LoadOwnedSessionAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (!sessionResult.IsSuccess)
        {
            return sessionResult;
        }

        try
        {
            sessionResult.Value!.EnsureModifiable(DateTime.UtcNow);
            return sessionResult;
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("ready for order", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<CheckoutSession>(Error.Validation(ex.Message));
            }

            return Result.Failure<CheckoutSession>(
                ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase)
                    ? CheckoutErrors.CheckoutExpired()
                    : Error.Validation(ex.Message));
        }
    }

    private async Task<CheckoutDto> MapAsync(CheckoutSession session, CancellationToken cancellationToken)
    {
        var displayKeys = session.Items
            .Select(x => (x.OfferId, x.ProductId, x.VariantId))
            .ToList();
        var images = await itemEnricher.GetImagesByOfferAsync(displayKeys, cancellationToken).ConfigureAwait(false);

        var itemDtos = new List<CheckoutItemDto>();
        foreach (var item in session.Items)
        {
            var validation = await offerValidator.ValidateLineAsync(
                item.OfferId,
                item.Quantity,
                session.StoreId,
                session.CurrencyId,
                session.CurrencyCode,
                item.PreviousUnitPrice,
                cancellationToken).ConfigureAwait(false);

            itemDtos.Add(new CheckoutItemDto(
                item.CartItemId,
                item.OfferId,
                validation.ProductId,
                validation.VariantId,
                validation.ProductName,
                validation.VariantName,
                validation.Sku,
                item.Quantity,
                item.UnitPrice,
                item.LineSubtotal,
                item.CurrencyCode,
                item.UnitPrice != item.PreviousUnitPrice,
                images.GetValueOrDefault(item.OfferId)));
        }

        var billing = session.BillingAddress is null ? null : MapAddressDto(session.BillingAddress);
        var shipping = session.ShippingAddress is null ? null : MapAddressDto(session.ShippingAddress);
        var shippingOptions = await GetShippingOptionsAsync(session, shipping, itemDtos, cancellationToken).ConfigureAwait(false);
        var paymentMethods = await GetPaymentMethodsAsync(session, cancellationToken).ConfigureAwait(false);

        var errors = new List<string>();
        var warnings = new List<string>();

        if (session.PriceChangeDetected || itemDtos.Any(x => x.PriceChanged))
        {
            warnings.Add("The price of one or more items has changed. Please review your cart.");
        }

        if (!session.CustomerId.HasValue && string.IsNullOrWhiteSpace(session.GuestEmail))
        {
            errors.Add("Guest email is required.");
        }

        if (session.BillingAddress is null)
        {
            errors.Add("Billing address is required.");
        }

        if (session.RequiresShipping && session.ShippingAddress is null)
        {
            errors.Add("Shipping address is required.");
        }

        if (session.RequiresShipping && shippingOptions.Count == 0)
        {
            warnings.Add("Shipping options are currently unavailable.");
        }
        else if (session.RequiresShipping && shippingOptions.Count > 0 && string.IsNullOrWhiteSpace(session.SelectedShippingMethodId))
        {
            errors.Add("Shipping method is required.");
        }

        if (paymentMethods.Count == 0)
        {
            warnings.Add("No payment methods are currently available.");
        }
        else if (string.IsNullOrWhiteSpace(session.SelectedPaymentMethodId))
        {
            errors.Add("Payment method is required.");
        }

        string? customerEmail = session.GuestEmail;
        if (session.CustomerId is int customerId)
        {
            var customer = await customerReader.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
            customerEmail = customer.Value?.Email ?? customerEmail;
        }

        var totals = new CheckoutTotalsDto(
            session.Subtotal,
            session.DiscountTotal,
            session.ShippingTotal,
            session.TaxTotal,
            session.GrandTotal,
            session.CurrencyCode);

        var status = session.Status;
        if (errors.Count == 0 && status is CheckoutStatus.Active or CheckoutStatus.RequiresReview && !session.PriceChangeDetected)
        {
            status = CheckoutStatus.Active;
        }

        return new CheckoutDto(
            session.Id,
            session.CartId,
            session.StoreId,
            status,
            session.CurrencyCode,
            session.CurrencyId,
            new CheckoutCustomerDto(session.CustomerId, customerEmail, !session.CustomerId.HasValue),
            billing,
            shipping,
            session.UseShippingAsBilling,
            session.RequiresShipping,
            session.PriceChangeDetected || itemDtos.Any(x => x.PriceChanged),
            itemDtos,
            totals,
            shippingOptions,
            paymentMethods,
            session.SelectedShippingMethodId,
            session.SelectedPaymentMethodId,
            errors,
            warnings,
            session.ExpiresAtUtc,
            session.CartUpdatedAtUtc);
    }

    private async Task<IReadOnlyList<ShippingOptionDto>> GetShippingOptionsAsync(
        CheckoutSession session,
        CheckoutAddressDto? shippingAddress,
        IReadOnlyList<CheckoutItemDto> items,
        CancellationToken cancellationToken)
    {
        if (!session.RequiresShipping || shippingAddress is null)
        {
            return [];
        }

        var request = new ShippingRateRequest(
            session.StoreId,
            session.CartId,
            session.CurrencyCode,
            shippingAddress,
            items.Select(x => new ShippingRateLineItem(
                x.OfferId,
                x.ProductId,
                x.VariantId,
                x.Quantity,
                x.UnitPrice,
                string.Empty)).ToList());

        var options = new List<ShippingOptionDto>();
        foreach (var provider in shippingProviders)
        {
            var rates = await provider.GetRatesAsync(request, cancellationToken).ConfigureAwait(false);
            options.AddRange(rates.Select(x => new ShippingOptionDto(
                x.Id,
                x.Name,
                x.ProviderSystemName,
                x.Price,
                x.Currency,
                x.EstimatedDelivery)));
        }

        return options;
    }

    private async Task<IReadOnlyList<PaymentMethodDto>> GetPaymentMethodsAsync(
        CheckoutSession session,
        CancellationToken cancellationToken)
    {
        var methods = new List<PaymentMethodDto>();
        foreach (var provider in paymentProviders)
        {
            var providerMethods = await provider.GetMethodsAsync(
                session.StoreId,
                session.CurrencyCode,
                !session.CustomerId.HasValue,
                cancellationToken).ConfigureAwait(false);
            methods.AddRange(providerMethods);
        }

        return methods;
    }

    private Result<CheckoutOwnershipContext> ResolveContext()
    {
        if (!storeContext.CurrentStoreId.HasValue)
        {
            return Result.Failure<CheckoutOwnershipContext>(CheckoutErrors.StoreContextRequired());
        }

        if (!storeContext.CurrentCurrencyId.HasValue || string.IsNullOrWhiteSpace(storeContext.CurrentCurrencyCode))
        {
            return Result.Failure<CheckoutOwnershipContext>(CheckoutErrors.CurrencyContextRequired());
        }

        return Result.Success(new CheckoutOwnershipContext(
            storeContext.CurrentStoreId.Value,
            storeContext.CurrentCurrencyId.Value,
            storeContext.CurrentCurrencyCode,
            currentCustomerContext.CustomerId,
            guestCartContext.GetGuestToken()));
    }

    private static CheckoutAddressSnapshot MapAddressSnapshot(CustomerAddressDto address) =>
        CheckoutAddressSnapshot.Create(
            address.FirstName,
            address.LastName,
            address.Country,
            address.City,
            address.Address1,
            address.PostalCode,
            address.StateProvince,
            address.Address2,
            address.PhoneNumber,
            address.Id);

    private static CheckoutAddressDto MapAddressDto(CheckoutAddressSnapshot address) =>
        new(
            address.SourceCustomerAddressId,
            address.FirstName,
            address.LastName,
            address.Country,
            address.StateProvince,
            address.City,
            address.Address1,
            address.Address2,
            address.PostalCode,
            address.PhoneNumber);

    private sealed record CheckoutOwnershipContext(
        int StoreId,
        int CurrencyId,
        string CurrencyCode,
        int? CustomerId,
        string? GuestToken);
}
