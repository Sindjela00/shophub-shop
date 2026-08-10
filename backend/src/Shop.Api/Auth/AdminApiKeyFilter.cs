using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shop.Api.Contracts;

namespace Shop.Api.Auth;

/// <summary>
/// Minimal admin gate for write actions: checks the caller sent the configured admin
/// API key via the X-Admin-Key header. Placeholder until the Shop app gets real admin
/// authentication.
/// </summary>
public class AdminApiKeyFilter(IConfiguration configuration) : IAsyncActionFilter
{
    private const string HeaderName = "X-Admin-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuredKey = configuration["Admin:ApiKey"];
        if (string.IsNullOrEmpty(configuredKey))
        {
            context.Result = new ObjectResult(new ErrorResponse("Admin API key is not configured."))
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            };
            return;
        }

        var providedKey = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (!string.Equals(providedKey, configuredKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
