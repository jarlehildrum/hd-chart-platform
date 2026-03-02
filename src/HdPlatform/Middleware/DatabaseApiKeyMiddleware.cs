using HdPlatform.Services;
using System.Diagnostics;

namespace HdPlatform.Middleware;

public class DatabaseApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DatabaseApiKeyMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DatabaseApiKeyMiddleware(RequestDelegate next, ILogger<DatabaseApiKeyMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for non-protected endpoints
        if (!RequiresAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<DatabaseApiKeyService>();

        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey))
        {
            await WriteUnauthorizedResponse(context, "Missing API key header");
            return;
        }

        // Validate API key
        var keyData = await apiKeyService.ValidateKeyAsync(apiKey);
        if (keyData == null)
        {
            await WriteUnauthorizedResponse(context, "Invalid API key");
            await TrackUsage(apiKeyService, apiKey, context.Request.Path, 0, false, "Invalid API key", context);
            return;
        }

        // Check rate limit
        var hasCapacity = await apiKeyService.CheckRateLimitAsync(apiKey);
        if (!hasCapacity)
        {
            await WriteRateLimitResponse(context, keyData.MonthlyLimit);
            await TrackUsage(apiKeyService, apiKey, context.Request.Path, 0, false, "Rate limit exceeded", context);
            return;
        }

        // Track request start time
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;
        
        try
        {
            // Continue with request
            await _next(context);
            
            stopwatch.Stop();
            var success = context.Response.StatusCode < 400;
            
            // Track successful request
            await TrackUsage(apiKeyService, apiKey, context.Request.Path, (int)stopwatch.ElapsedMilliseconds, success, null, context);
            
            _logger.LogInformation("API call authorized for {Tier} key: {Path} ({Ms}ms)", 
                keyData.Tier, context.Request.Path, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Track failed request
            await TrackUsage(apiKeyService, apiKey, context.Request.Path, (int)stopwatch.ElapsedMilliseconds, false, ex.Message, context);
            
            _logger.LogError(ex, "Error processing request for API key {ApiKey}", apiKey);
            throw;
        }
    }

    private static async Task TrackUsage(DatabaseApiKeyService apiKeyService, string apiKey, string endpoint, 
        int responseTimeMs, bool success, string? errorMessage, HttpContext context)
    {
        try
        {
            var ipAddress = GetClientIpAddress(context);
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
            
            await apiKeyService.IncrementUsageAsync(apiKey, endpoint, responseTimeMs, success, errorMessage, ipAddress, userAgent);
        }
        catch (Exception ex)
        {
            // Don't let tracking errors break the request
            var logger = context.RequestServices.GetService<ILogger<DatabaseApiKeyMiddleware>>();
            logger?.LogError(ex, "Error tracking API usage");
        }
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP (behind load balancer/proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static bool RequiresAuthentication(string path)
    {
        var protectedPaths = new[]
        {
            "/api/chart",
            "/api/chart/utc", 
            "/api/chart/transit",
            "/api/chart/composite",
            "/api/chart/image"
        };

        return protectedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync($"{{\"error\":\"{message}\",\"statusCode\":401}}");
    }

    private static async Task WriteRateLimitResponse(HttpContext context, int monthlyLimit)
    {
        context.Response.StatusCode = 429;
        context.Response.ContentType = "application/json";
        context.Response.Headers.Add("X-RateLimit-Limit", monthlyLimit.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", "0");
        context.Response.Headers.Add("Retry-After", "3600");
        
        await context.Response.WriteAsync($"{{\"error\":\"Rate limit exceeded. Monthly limit: {monthlyLimit} requests.\",\"statusCode\":429,\"monthlyLimit\":{monthlyLimit}}}");
    }
}