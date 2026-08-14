using Commerce.Catalog.Contracts.Offers;
using Commerce.Catalog.Contracts.Pricing;
using Commerce.Customers.Contracts.Customers;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Pricing.Application.Abstractions;
using Commerce.Pricing.Contracts.AdvancedPricing;
using Commerce.Pricing.Contracts.Pricing;
using Commerce.Pricing.Domain.Entities;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Pricing.Application.AdvancedPricing;

public sealed class AdvancedPricingService(
    ICatalogPricingReader catalogPricingReader,
    IProductPricingPipeline pricingPipeline,
    IPriceCalculationService priceCalculationService,
    ICustomerReader customerReader,
    IStoreContext storeContext,
    ICurrentCustomerContext customerContext) : IAdvancedPricingService
{
    public async Task<PricePreviewResult> PreviewAsync(
        PricePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var storeId = storeContext.CurrentStoreId
            ?? throw new InvalidOperationException("Store context is required.");

        var basePrice = await catalogPricingReader.GetOfferPriceAsync(request.OfferId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Offer price could not be resolved.");

        var customerGroupId = request.CustomerGroupId;
        if (!customerGroupId.HasValue && request.CustomerId.HasValue)
        {
            var customer = await customerReader.GetByIdAsync(request.CustomerId.Value, cancellationToken).ConfigureAwait(false);
            customerGroupId = customer.IsSuccess ? customer.Value?.CustomerGroupId : null;
        }
        else if (!customerGroupId.HasValue && customerContext.CustomerId.HasValue)
        {
            var customer = await customerReader.GetByIdAsync(customerContext.CustomerId.Value, cancellationToken).ConfigureAwait(false);
            customerGroupId = customer.IsSuccess ? customer.Value?.CustomerGroupId : null;
        }

        var pipelineResult = await pricingPipeline.ResolveUnitPriceAsync(
            new ProductPricingContext(
                storeId,
                request.OfferId,
                basePrice.ProductId,
                basePrice.VariantId,
                request.Quantity,
                request.CurrencyCode,
                CurrencyId: 0,
                basePrice.UnitPrice,
                basePrice.CompareAtPrice,
                request.CustomerId ?? customerContext.CustomerId,
                customerGroupId,
                DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);

        var discountResult = await priceCalculationService.CalculateOfferPriceAsync(
            new PriceCalculationContext(
                storeId,
                pipelineResult.CurrencyCode,
                request.CustomerId ?? customerContext.CustomerId,
                !(request.CustomerId ?? customerContext.CustomerId).HasValue,
                customerGroupId,
                request.OfferId,
                basePrice.ProductId,
                basePrice.VariantId,
                request.Quantity,
                pipelineResult.AdjustedUnitPrice,
                pipelineResult.AdjustedUnitPrice * request.Quantity,
                CouponCode: null,
                DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);

        return new PricePreviewResult(
            pipelineResult.BaseUnitPrice,
            pipelineResult.AdjustedUnitPrice,
            pipelineResult.CompareAtPrice,
            discountResult.FinalPrice,
            discountResult.DiscountAmount,
            pipelineResult.CurrencyCode,
            pipelineResult.TierPriceApplied,
            pipelineResult.CustomerGroupPriceApplied,
            pipelineResult.CurrencyConverted);
    }
}

public sealed class CustomerGroupAdminService(IPricingRepository repository) : ICustomerGroupAdminService
{
    public async Task<IReadOnlyList<CustomerGroupDto>> ListGroupsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var groups = await repository.ListCustomerGroupsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return groups.Select(MapGroup).ToList();
    }

    public async Task<CustomerGroupDto?> GetGroupAsync(int id, CancellationToken cancellationToken = default)
    {
        var group = await repository.GetCustomerGroupAsync(id, cancellationToken).ConfigureAwait(false);
        return group is null ? null : MapGroup(group);
    }

    public async Task<CustomerGroupDto> CreateGroupAsync(CreateCustomerGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = CustomerGroup.Create(request.StoreId, request.Name, request.Code, request.IsActive, request.DisplayOrder);
        await repository.AddCustomerGroupAsync(group, cancellationToken).ConfigureAwait(false);
        return MapGroup(group);
    }

    public async Task<CustomerGroupDto> UpdateGroupAsync(int id, UpdateCustomerGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await repository.GetCustomerGroupAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Customer group not found.");
        group.Update(request.Name, request.Code, request.IsActive, request.DisplayOrder);
        await repository.SaveCustomerGroupAsync(group, cancellationToken).ConfigureAwait(false);
        return MapGroup(group);
    }

    public async Task DeleteGroupAsync(int id, CancellationToken cancellationToken = default)
    {
        var group = await repository.GetCustomerGroupAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Customer group not found.");
        await repository.DeleteCustomerGroupAsync(group, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CustomerGroupPriceDto>> ListGroupPricesAsync(int groupId, CancellationToken cancellationToken = default)
    {
        var prices = await repository.ListCustomerGroupPricesAsync(groupId, cancellationToken).ConfigureAwait(false);
        return prices.Select(MapPrice).ToList();
    }

    public async Task<CustomerGroupPriceDto> AddGroupPriceAsync(CreateCustomerGroupPriceRequest request, CancellationToken cancellationToken = default)
    {
        var money = Money.Create(request.Price, Currency.FromCode(request.CurrencyCode));
        var price = CustomerGroupPrice.Create(
            request.CustomerGroupId,
            request.StoreId,
            request.ProductId,
            request.VariantId,
            request.CurrencyId,
            request.CurrencyCode,
            money,
            request.IsActive);
        await repository.AddCustomerGroupPriceAsync(price, cancellationToken).ConfigureAwait(false);
        return MapPrice(price);
    }

    public async Task<CustomerGroupPriceDto> UpdateGroupPriceAsync(int id, UpdateCustomerGroupPriceRequest request, CancellationToken cancellationToken = default)
    {
        var price = await repository.GetCustomerGroupPriceByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Customer group price not found.");
        price.Update(Money.Create(request.Price, Currency.FromCode(price.CurrencyCode)), request.IsActive);
        await repository.SaveCustomerGroupPriceAsync(price, cancellationToken).ConfigureAwait(false);
        return MapPrice(price);
    }

    public async Task DeleteGroupPriceAsync(int id, CancellationToken cancellationToken = default)
    {
        var price = await repository.GetCustomerGroupPriceByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Customer group price not found.");
        await repository.DeleteCustomerGroupPriceAsync(price, cancellationToken).ConfigureAwait(false);
    }

    private static CustomerGroupDto MapGroup(CustomerGroup group) =>
        new(group.Id, group.StoreId, group.Name, group.Code, group.IsActive, group.DisplayOrder, group.CreatedAtUtc, group.UpdatedAtUtc);

    private static CustomerGroupPriceDto MapPrice(CustomerGroupPrice price) =>
        new(price.Id, price.CustomerGroupId, price.StoreId, price.ProductId, price.VariantId, price.CurrencyId, price.CurrencyCode, price.Price, price.IsActive);
}
