using Commerce.Host.Authorization;

using Commerce.Tax.Contracts.Admin;

using Commerce.Tax.Infrastructure.Security;

using Microsoft.AspNetCore.Mvc;



namespace Commerce.Host.Tax;



[ApiController]

[Route("api/admin/tax/categories")]

public sealed class AdminTaxCategoriesController(ITaxAdminService taxAdminService) : ControllerBase

{

    [HttpGet]

    [RequirePermission(TaxPermissions.View)]

    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.ListCategoriesAsync(storeId, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value);

    }



    [HttpGet("{id:int}")]

    [RequirePermission(TaxPermissions.View)]

    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.GetCategoryAsync(id, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value);

    }



    [HttpPost]

    [RequirePermission(TaxPermissions.Manage)]

    public async Task<IActionResult> Create([FromBody] CreateTaxCategoryRequest request, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.CreateCategoryAsync(request, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);

    }



    [HttpPut("{id:int}")]

    [RequirePermission(TaxPermissions.Manage)]

    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaxCategoryRequest request, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.UpdateCategoryAsync(id, request, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value);

    }



    [HttpDelete("{id:int}")]

    [RequirePermission(TaxPermissions.Manage)]

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.DeleteCategoryAsync(id, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result);

    }

}



[ApiController]

[Route("api/admin/tax/zones")]

public sealed class AdminTaxZonesController(ITaxAdminService taxAdminService) : ControllerBase

{

    [HttpGet]

    [RequirePermission(TaxPermissions.View)]

    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.ListZonesAsync(storeId, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value);

    }



    [HttpGet("{id:int}")]

    [RequirePermission(TaxPermissions.View)]

    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.GetZoneAsync(id, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value);

    }



    [HttpPost]

    [RequirePermission(TaxPermissions.Manage)]

    public async Task<IActionResult> Create([FromBody] CreateTaxZoneRequest request, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.CreateZoneAsync(request, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);

    }



    [HttpPut("{id:int}")]

    [RequirePermission(TaxPermissions.Manage)]

    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaxZoneRequest request, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.UpdateZoneAsync(id, request, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value);

    }



    [HttpDelete("{id:int}")]

    [RequirePermission(TaxPermissions.Manage)]

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.DeleteZoneAsync(id, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result);

    }

}



[ApiController]

[Route("api/admin/tax/rates")]

public sealed class AdminTaxRatesController(ITaxAdminService taxAdminService) : ControllerBase

{

    [HttpGet]

    [RequirePermission(TaxPermissions.View)]

    public async Task<IActionResult> List(

        [FromQuery] int? storeId,

        [FromQuery] int? categoryId,

        CancellationToken cancellationToken)

    {

        var result = await taxAdminService.ListRatesAsync(storeId, categoryId, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value);

    }



    [HttpGet("{id:int}")]

    [RequirePermission(TaxPermissions.View)]

    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.GetRateAsync(id, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value);

    }



    [HttpPost]

    [RequirePermission(TaxPermissions.Manage)]

    public async Task<IActionResult> Create([FromBody] CreateTaxRateRequest request, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.CreateRateAsync(request, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);

    }



    [HttpPut("{id:int}")]

    [RequirePermission(TaxPermissions.Manage)]

    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaxRateRequest request, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.UpdateRateAsync(id, request, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result, value => value);

    }



    [HttpDelete("{id:int}")]

    [RequirePermission(TaxPermissions.Manage)]

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)

    {

        var result = await taxAdminService.DeleteRateAsync(id, cancellationToken).ConfigureAwait(false);

        return TaxActionResults.ToActionResult(this, result);

    }

}



internal static class TaxActionResults

{

    internal static IActionResult ToActionResult<T>(

        ControllerBase controller,

        Commerce.Framework.Core.Results.Result<T> result,

        Func<T, object?> dataSelector,

        int successStatusCode = StatusCodes.Status200OK)

    {

        if (result.IsSuccess)

        {

            return controller.StatusCode(successStatusCode, new { success = true, data = dataSelector(result.Value!) });

        }



        return MapFailure(controller, result.Error!);

    }



    internal static IActionResult ToActionResult(

        ControllerBase controller,

        Commerce.Framework.Core.Results.Result result)

    {

        if (result.IsSuccess)

        {

            return controller.Ok(new { success = true });

        }



        return MapFailure(controller, result.Error!);

    }



    private static IActionResult MapFailure(ControllerBase controller, Commerce.Framework.Core.Errors.Error error) =>

        error.Type switch

        {

            Commerce.Framework.Core.Errors.ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),

            Commerce.Framework.Core.Errors.ErrorType.Conflict => controller.Conflict(new { success = false, error = error.Message }),

            _ => controller.BadRequest(new { success = false, error = error.Message })

        };

}


