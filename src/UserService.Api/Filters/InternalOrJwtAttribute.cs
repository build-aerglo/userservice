// UserService.Api/Filters/InternalOrJwtAttribute.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class InternalOrJwtAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeader = "X-Internal-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Path 1: valid Internal API key → allow
        if (context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeader, out var extractedKey))
        {
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var configuredKey = config["InternalApiKey"];
            if (!string.IsNullOrWhiteSpace(configuredKey) && extractedKey == configuredKey)
            {
                await next();
                return;
            }
        }

        // Path 2: valid JWT (any authenticated user) → allow
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            await next();
            return;
        }

        context.Result = new UnauthorizedObjectResult("Authentication required.");
    }
}