using HdPlatform.Services;

namespace HdPlatform.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ApiKeyService keyService)
    {
        var path = context.Request.Path.Value ?? "";

        // Public endpoints - no auth required
        if (IsPublicEndpoint(path))
        {
            await _next(context);
            return;
        }

        // Chart endpoints require API key
        if (path.StartsWith("/api/chart"))
        {
            await ValidateApiKey(context, keyService);
            return;
        }

        // Continue for other endpoints
        await _next(context);
    }

    private static bool IsPublicEndpoint(string path) =>
        path == "/" || 
        path == "/api" || 
        path == "/api/health" ||
        path.StartsWith("/docs") ||
        path.StartsWith("/swagger") ||
        path.StartsWith("/api/demo") ||
        path.StartsWith("/api/signup") ||
        path.StartsWith("/api/admin");

    private async Task ValidateApiKey(HttpContext context, ApiKeyService keyService)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault()
                     ?? context.Request.Query["api_key"].FirstOrDefault();

        var keyInfo = keyService.Validate(apiKey);
        if (keyInfo == null)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Invalid or missing API key. Include X-API-Key header or api_key query parameter.\"}");
            return;
        }

        var (allowed, used, limit) = keyService.CheckRateLimit(apiKey!);
        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Used"] = used.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - used).ToString();

        if (!allowed)
        {
            context.Response.StatusCode = 429;
            context.Response.ContentType = "application/json";
            var errorMsg = "{\"error\":\"Monthly rate limit exceeded (" + used + "/" + limit + "). Upgrade your plan.\",\"tier\":\"" + keyInfo.Tier + "\"}";
            await context.Response.WriteAsync(errorMsg);
            return;
        }

        keyService.RecordUsage(apiKey!);
        context.Items["ApiKey"] = keyInfo;
        
        _logger.LogInformation("API call authorized for {Tier} key: {Path}", keyInfo.Tier, context.Request.Path);
        await _next(context);
    }
}