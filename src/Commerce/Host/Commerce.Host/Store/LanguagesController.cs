using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Commerce.Store.Application.Languages;
using Commerce.Store.Infrastructure.Middleware;
using Commerce.Store.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Store;

[ApiController]
[Route("api/languages")]
public sealed class LanguagesController(ILanguageService languageService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await languageService.ListAsync(includeInactive: false, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(StorePermissions.LanguagesView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await languageService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost]
    [RequirePermission(StorePermissions.LanguagesCreate)]
    public async Task<IActionResult> Create([FromBody] CreateLanguageRequest request, CancellationToken cancellationToken)
    {
        var result = await languageService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value, createdId: value => value.Id);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(StorePermissions.LanguagesUpdate)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLanguageRequest request, CancellationToken cancellationToken)
    {
        var result = await languageService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost("select/{languageCode}")]
    [AllowAnonymous]
    public IActionResult SelectLanguage(string languageCode)
    {
        Response.Cookies.Append(
            StoreContextMiddleware.LanguageCookieName,
            languageCode,
            new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(365),
                IsEssential = true
            });

        return Ok(new { success = true });
    }

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

    private IActionResult MapFailure(Error error) =>
        error.Type switch
        {
            ErrorType.NotFound => NotFound(new { success = false, error = error.Message }),
            ErrorType.Conflict => Conflict(new { success = false, error = error.Message }),
            _ => BadRequest(new { success = false, error = error.Message })
        };
}
