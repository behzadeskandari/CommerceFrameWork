using Commerce.Framework.PluginContracts.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Plugin.Test.Controllers;

[ApiController]
[PluginController("Commerce.Test")]
[Route("ping")]
public sealed class TestPluginController : ControllerBase
{
    [HttpGet]
    public IActionResult Ping() =>
        Ok(new { plugin = "Commerce.Test", status = "ok" });
}
