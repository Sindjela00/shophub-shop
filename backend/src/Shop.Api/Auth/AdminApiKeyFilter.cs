namespace Shop.Api.Auth;

/// <summary>
/// Minimal admin gate for write endpoints: checks the caller sent the configured admin
/// API key via the X-Admin-Key header. Placeholder until the Shop app gets real admin
/// authentication.
/// </summary>
public class AdminApiKeyFilter(IConfiguration configuration) : IEndpointFilter
{
    private const string HeaderName = "X-Admin-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configuredKey = configuration["Admin:ApiKey"];
        if (string.IsNullOrEmpty(configuredKey))
        {
            return Results.Problem("Admin API key is not configured.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var providedKey = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (!string.Equals(providedKey, configuredKey, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
