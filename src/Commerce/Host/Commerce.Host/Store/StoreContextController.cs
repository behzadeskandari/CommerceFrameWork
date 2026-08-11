using Commerce.Framework.Contracts.Tenancy;
using Commerce.Store.Contracts.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Store;

[ApiController]
[Route("api/store")]
public sealed class StoreContextController(IStoreContext storeContext) : ControllerBase
{
    [HttpGet("context")]
    [AllowAnonymous]
    public IActionResult GetContext()
    {
        var dto = new StoreContextDto(
            storeContext.CurrentStoreId,
            storeContext.CurrentStoreSystemName,
            storeContext.CurrentStoreName,
            storeContext.CurrentLanguageId,
            storeContext.CurrentLanguageCode,
            storeContext.CurrentCultureCode,
            storeContext.IsRtl,
            storeContext.CurrentCurrencyId,
            storeContext.CurrentCurrencyCode);

        return Ok(new { success = true, data = dto });
    }
}
