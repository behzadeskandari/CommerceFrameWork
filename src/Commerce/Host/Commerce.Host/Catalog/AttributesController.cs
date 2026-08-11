using Commerce.Catalog.Application.Attributes;
using Commerce.Catalog.Domain.Enums;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Catalog;

[ApiController]
[Route("api/catalog/attributes")]
public sealed class AttributesController(IAttributeService attributeService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Catalog.Attributes.View")]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var result = await attributeService.ListAsync(includeInactive, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("Catalog.Attributes.View")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await attributeService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost]
    [RequirePermission("Catalog.Attributes.Create")]
    public async Task<IActionResult> Create([FromBody] CreateAttributeDefinitionApiRequest request, CancellationToken cancellationToken)
    {
        var result = await attributeService.CreateDefinitionAsync(new CreateAttributeDefinitionRequest(
            request.Name,
            request.Code,
            request.AttributeType,
            request.DisplayOrder,
            request.IsActive), cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value, createdId: value => value.Id);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Catalog.Attributes.Update")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAttributeDefinitionApiRequest request, CancellationToken cancellationToken)
    {
        var result = await attributeService.UpdateDefinitionAsync(id, new UpdateAttributeDefinitionRequest(
            request.Name,
            request.AttributeType,
            request.DisplayOrder,
            request.IsActive), cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value);
    }

    [HttpPost("{attributeId:int}/options")]
    [RequirePermission("Catalog.Attributes.Create")]
    public async Task<IActionResult> CreateOption(int attributeId, [FromBody] CreateAttributeOptionApiRequest request, CancellationToken cancellationToken)
    {
        var result = await attributeService.CreateOptionAsync(new CreateAttributeOptionRequest(
            attributeId,
            request.Value,
            request.DisplayOrder,
            request.IsActive), cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value, createdId: value => value.Id);
    }

    [HttpPut("options/{optionId:int}")]
    [RequirePermission("Catalog.Attributes.Update")]
    public async Task<IActionResult> UpdateOption(int optionId, [FromBody] UpdateAttributeOptionApiRequest request, CancellationToken cancellationToken)
    {
        var result = await attributeService.UpdateOptionAsync(optionId, new UpdateAttributeOptionRequest(
            request.Value,
            request.DisplayOrder,
            request.IsActive), cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value);
    }

    [HttpGet("products/{productId:int}")]
    [RequirePermission("Catalog.Attributes.View")]
    public async Task<IActionResult> GetForProduct(int productId, CancellationToken cancellationToken)
    {
        var result = await attributeService.GetForProductAsync(productId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost("products/{productId:int}/{attributeId:int}")]
    [RequirePermission("Catalog.Attributes.Update")]
    public async Task<IActionResult> AssignToProduct(int productId, int attributeId, CancellationToken cancellationToken)
    {
        var result = await attributeService.AssignToProductAsync(
            new AssignProductAttributeRequest(productId, attributeId),
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    [HttpDelete("products/{productId:int}/{attributeId:int}")]
    [RequirePermission("Catalog.Attributes.Delete")]
    public async Task<IActionResult> RemoveFromProduct(int productId, int attributeId, CancellationToken cancellationToken)
    {
        var result = await attributeService.RemoveFromProductAsync(productId, attributeId, cancellationToken)
            .ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPut("products/{productId:int}/values")]
    [RequirePermission("Catalog.Attributes.Update")]
    public async Task<IActionResult> SetProductValue(
        int productId,
        [FromBody] SetProductAttributeValueApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await attributeService.SetProductValueAsync(
            new SetProductAttributeValueRequest(productId, request.AttributeDefinitionId, request.Value),
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    private IActionResult ToActionResult(Result result) =>
        result.IsSuccess ? Ok(new { success = true }) : MapFailure(result.Error!);

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, object?> dataSelector, Func<T, int>? createdId = null)
    {
        if (result.IsSuccess)
        {
            if (createdId is not null)
            {
                return CreatedAtAction(nameof(Get), new { id = createdId(result.Value!) }, new { success = true, data = dataSelector(result.Value!) });
            }

            return Ok(new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(result.Error!);
    }

    private IActionResult MapFailure(Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Conflict => Conflict(new { success = false, error = error.Message }),
            _ => BadRequest(new { success = false, error = error.Message })
        };
}

public sealed record CreateAttributeDefinitionApiRequest(
    string Name,
    string Code,
    AttributeType AttributeType,
    int DisplayOrder = 0,
    bool IsActive = true);

public sealed record UpdateAttributeDefinitionApiRequest(
    string Name,
    AttributeType AttributeType,
    int DisplayOrder = 0,
    bool IsActive = true);

public sealed record CreateAttributeOptionApiRequest(string Value, int DisplayOrder = 0, bool IsActive = true);

public sealed record UpdateAttributeOptionApiRequest(string Value, int DisplayOrder = 0, bool IsActive = true);

public sealed record SetProductAttributeValueApiRequest(int AttributeDefinitionId, string Value);
