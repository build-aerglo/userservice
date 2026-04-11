using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UserService.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class InternalApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeader = "X-Internal-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeader, out var extractedKey))
        {
            context.Result = new UnauthorizedObjectResult("Internal API Key missing.");
            return;
        }

        var configuredKey = config["InternalApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey) || extractedKey != configuredKey)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}   