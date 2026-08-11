using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Contracts.Attributes;
using Commerce.Catalog.Domain.Entities;
using Commerce.Catalog.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Catalog.Application.Attributes;

public interface IAttributeService : IProductAttributeReader
{
    Task<Result<AttributeDefinitionDto>> CreateDefinitionAsync(
        CreateAttributeDefinitionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AttributeDefinitionDto>> UpdateDefinitionAsync(
        int attributeId,
        UpdateAttributeDefinitionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AttributeOptionDto>> CreateOptionAsync(
        CreateAttributeOptionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AttributeOptionDto>> UpdateOptionAsync(
        int optionId,
        UpdateAttributeOptionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> AssignToProductAsync(
        AssignProductAttributeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveFromProductAsync(int productId, int attributeDefinitionId, CancellationToken cancellationToken = default);

    Task<Result> SetProductValueAsync(
        SetProductAttributeValueRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class AttributeService(
    IProductRepository productRepository,
    IProductAttributeRepository attributeRepository) : IAttributeService
{
    public async Task<Result<AttributeDefinitionDto>> CreateDefinitionAsync(
        CreateAttributeDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (await attributeRepository.GetDefinitionByCodeAsync(request.Code, cancellationToken).ConfigureAwait(false) is not null)
            {
                return Result.Failure<AttributeDefinitionDto>(
                    Error.Conflict($"Attribute code '{request.Code}' already exists."));
            }

            var definition = ProductAttributeDefinition.Create(
                request.Name,
                request.Code,
                request.AttributeType,
                request.DisplayOrder,
                request.IsActive);

            await attributeRepository.AddDefinitionAsync(definition, cancellationToken).ConfigureAwait(false);
            return Result.Success(await MapDefinitionAsync(definition, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<AttributeDefinitionDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<AttributeDefinitionDto>> UpdateDefinitionAsync(
        int attributeId,
        UpdateAttributeDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = await attributeRepository.GetDefinitionByIdAsync(attributeId, cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return Result.Failure<AttributeDefinitionDto>(Error.NotFound($"Attribute '{attributeId}' was not found."));
        }

        try
        {
            definition.Update(request.Name, request.AttributeType, request.DisplayOrder, request.IsActive);
            await attributeRepository.UpdateDefinitionAsync(definition, cancellationToken).ConfigureAwait(false);
            return Result.Success(await MapDefinitionAsync(definition, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<AttributeDefinitionDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<AttributeOptionDto>> CreateOptionAsync(
        CreateAttributeOptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await attributeRepository.GetDefinitionByIdAsync(request.AttributeDefinitionId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result.Failure<AttributeOptionDto>(
                Error.NotFound($"Attribute '{request.AttributeDefinitionId}' was not found."));
        }

        try
        {
            var option = ProductAttributeOption.Create(
                request.AttributeDefinitionId,
                request.Value,
                request.DisplayOrder,
                request.IsActive);

            await attributeRepository.AddOptionAsync(option, cancellationToken).ConfigureAwait(false);
            return Result.Success(MapOption(option));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<AttributeOptionDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<AttributeOptionDto>> UpdateOptionAsync(
        int optionId,
        UpdateAttributeOptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var option = await attributeRepository.GetOptionByIdAsync(optionId, cancellationToken).ConfigureAwait(false);
        if (option is null)
        {
            return Result.Failure<AttributeOptionDto>(Error.NotFound($"Attribute option '{optionId}' was not found."));
        }

        try
        {
            option.Update(request.Value, request.DisplayOrder, request.IsActive);
            await attributeRepository.UpdateOptionAsync(option, cancellationToken).ConfigureAwait(false);
            return Result.Success(MapOption(option));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<AttributeOptionDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> AssignToProductAsync(
        AssignProductAttributeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure(Error.NotFound($"Product '{request.ProductId}' was not found."));
        }

        var definition = await attributeRepository.GetDefinitionByIdAsync(request.AttributeDefinitionId, cancellationToken)
            .ConfigureAwait(false);
        if (definition is null)
        {
            return Result.Failure(Error.NotFound($"Attribute '{request.AttributeDefinitionId}' was not found."));
        }

        var existing = await attributeRepository.GetAssignmentsForProductAsync(request.ProductId, cancellationToken)
            .ConfigureAwait(false);
        if (existing.Any(x => x.AttributeDefinitionId == request.AttributeDefinitionId))
        {
            return Result.Failure(Error.Conflict("Attribute is already assigned to this product."));
        }

        await attributeRepository.AddAssignmentAsync(
            ProductAttributeAssignment.Create(request.ProductId, request.AttributeDefinitionId, request.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    public async Task<Result> RemoveFromProductAsync(
        int productId,
        int attributeDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure(Error.NotFound($"Product '{productId}' was not found."));
        }

        await attributeRepository.RemoveAssignmentAsync(productId, attributeDefinitionId, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> SetProductValueAsync(
        SetProductAttributeValueRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure(Error.NotFound($"Product '{request.ProductId}' was not found."));
        }

        var definition = await attributeRepository.GetDefinitionByIdAsync(request.AttributeDefinitionId, cancellationToken)
            .ConfigureAwait(false);
        if (definition is null)
        {
            return Result.Failure(Error.NotFound($"Attribute '{request.AttributeDefinitionId}' was not found."));
        }

        if (definition.AttributeType != AttributeType.Text &&
            definition.AttributeType != AttributeType.Number &&
            definition.AttributeType != AttributeType.Boolean)
        {
            return Result.Failure(Error.Validation("Only text, number, and boolean attributes support direct values."));
        }

        try
        {
            await attributeRepository.AddOrUpdateValueAsync(
                ProductAttributeValue.Create(request.ProductId, request.AttributeDefinitionId, request.Value),
                cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<AttributeDefinitionDto>> GetByIdAsync(int attributeId, CancellationToken cancellationToken = default)
    {
        var definition = await attributeRepository.GetDefinitionByIdAsync(attributeId, cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return Result.Failure<AttributeDefinitionDto>(Error.NotFound($"Attribute '{attributeId}' was not found."));
        }

        return Result.Success(await MapDefinitionAsync(definition, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<IReadOnlyList<AttributeDefinitionDto>>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var definitions = await attributeRepository.ListDefinitionsAsync(includeInactive, cancellationToken).ConfigureAwait(false);
        var mapped = new List<AttributeDefinitionDto>();
        foreach (var definition in definitions)
        {
            mapped.Add(await MapDefinitionAsync(definition, cancellationToken).ConfigureAwait(false));
        }

        return Result.Success<IReadOnlyList<AttributeDefinitionDto>>(mapped);
    }

    public async Task<Result<IReadOnlyList<ProductAttributeAssignmentDto>>> GetForProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure<IReadOnlyList<ProductAttributeAssignmentDto>>(
                Error.NotFound($"Product '{productId}' was not found."));
        }

        var assignments = await attributeRepository.GetAssignmentsForProductAsync(productId, cancellationToken)
            .ConfigureAwait(false);

        var mapped = new List<ProductAttributeAssignmentDto>();
        foreach (var assignment in assignments)
        {
            var definition = await attributeRepository.GetDefinitionByIdAsync(assignment.AttributeDefinitionId, cancellationToken)
                .ConfigureAwait(false);
            if (definition is null)
            {
                continue;
            }

            var options = await attributeRepository
                .GetOptionsForDefinitionAsync(definition.Id, includeInactive: false, cancellationToken)
                .ConfigureAwait(false);

            mapped.Add(new ProductAttributeAssignmentDto(
                definition.Id,
                definition.Code,
                definition.Name,
                definition.AttributeType.ToString(),
                options.Select(MapOption).ToList()));
        }

        return Result.Success<IReadOnlyList<ProductAttributeAssignmentDto>>(mapped);
    }

    private async Task<AttributeDefinitionDto> MapDefinitionAsync(
        ProductAttributeDefinition definition,
        CancellationToken cancellationToken)
    {
        var options = await attributeRepository
            .GetOptionsForDefinitionAsync(definition.Id, includeInactive: true, cancellationToken)
            .ConfigureAwait(false);

        return new AttributeDefinitionDto(
            definition.Id,
            definition.Name,
            definition.Code,
            definition.AttributeType.ToString(),
            definition.IsActive,
            definition.DisplayOrder,
            options.Select(MapOption).ToList());
    }

    private static AttributeOptionDto MapOption(ProductAttributeOption option) =>
        new(option.Id, option.AttributeDefinitionId, option.Value, option.IsActive, option.DisplayOrder);
}
