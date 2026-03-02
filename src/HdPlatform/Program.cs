using HdPlatform.Models;
using HdPlatform.Services;
using HdPlatform.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ApiKeyService>();
builder.Services.AddScoped<HdChartApiClient>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Human Design Chart API",
        Version = "v1",
        Description = "Professional Human Design chart calculations via REST API. Built by a certified BG5 consultant.",
        Contact = new OpenApiContact { Name = "HD Chart API", Email = "hello@hdchartapi.com" }
    });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey,
        Description = "API key for authentication. Get yours at the signup endpoint."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

// Middleware pipeline
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HD Chart API v1");
    c.RoutePrefix = "docs";
});
app.UseMiddleware<ApiKeyMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();

var adminSecret = builder.Configuration.GetValue<string>("AdminSecret") ?? Environment.GetEnvironmentVariable("HD_ADMIN_SECRET") ?? "changeme";

// ═══════════════════════════════════════════════════════════════════════════════════════
// PUBLIC ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════════════

app.MapGet("/api", () => Results.Ok(new
{
    name = "Human Design Chart API",
    version = "v1",
    description = "Professional HD chart calculations",
    docs = "/docs",
    status = "operational",
    built_by = "Certified BG5 consultant"
})).WithTags("Info").WithDescription("API information and status");

app.MapGet("/api/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    uptime = Environment.TickCount64 / 1000.0
})).WithTags("Info").WithDescription("Health check endpoint");

// ═══════════════════════════════════════════════════════════════════════════════════════
// CHART ENDPOINTS (Require API Key)
// ═══════════════════════════════════════════════════════════════════════════════════════

app.MapPost("/api/chart", async (ChartRequest request, HdChartApiClient chartApi) =>
{
    var result = await chartApi.CalculateChartAsync(request);
    return result.Success ? Results.Ok(result.Data) : Results.Problem(result.Error);
})
.WithTags("Charts")
.WithDescription("Calculate natal chart with automatic geocoding and timezone conversion")
.Accepts<ChartRequest>("application/json")
.Produces(200)
.ProducesProblem(400)
.ProducesProblem(401)
.ProducesProblem(429);

app.MapPost("/api/chart/utc", async (ChartRequest request, HdChartApiClient chartApi) =>
{
    var result = await chartApi.CalculateUtcChartAsync(request);
    return result.Success ? Results.Ok(result.Data) : Results.Problem(result.Error);
})
.WithTags("Charts")
.WithDescription("Calculate natal chart from UTC birth time (no geocoding needed)")
.Accepts<ChartRequest>("application/json")
.Produces(200);

app.MapPost("/api/chart/transit", async (TransitRequest request, HdChartApiClient chartApi) =>
{
    var result = await chartApi.CalculateTransitAsync(request);
    return result.Success ? Results.Ok(result.Data) : Results.Problem(result.Error);
})
.WithTags("Charts")
.WithDescription("Calculate natal chart with transit overlay for specific date")
.Accepts<TransitRequest>("application/json")
.Produces(200);

app.MapPost("/api/chart/composite", async (CompositeRequest request, HdChartApiClient chartApi) =>
{
    var result = await chartApi.CalculateCompositeAsync(request);
    return result.Success ? Results.Ok(result.Data) : Results.Problem(result.Error);
})
.WithTags("Charts")
.WithDescription("Calculate composite (relationship) chart between two people")
.Accepts<CompositeRequest>("application/json")
.Produces(200);

app.MapPost("/api/chart/image", async (ImageRequest request, HdChartApiClient chartApi) =>
{
    var imageData = await chartApi.GenerateImageAsync(request);
    return imageData != null 
        ? Results.File(imageData, "image/png", "bodygraph.png")
        : Results.Problem("Failed to generate chart image");
})
.WithTags("Charts")
.WithDescription("Generate professional bodygraph PNG image")
.Accepts<ImageRequest>("application/json")
.Produces(200, contentType: "image/png");

// ═══════════════════════════════════════════════════════════════════════════════════════
// DEMO ENDPOINTS (No Authentication)
// ═══════════════════════════════════════════════════════════════════════════════════════

app.MapPost("/api/demo/chart", async (ChartRequest request, HdChartApiClient chartApi) =>
{
    var result = await chartApi.CalculateChartAsync(request);
    return result.Success ? Results.Ok(result.Data) : Results.Problem(result.Error);
})
.WithTags("Demo")
.WithDescription("Demo chart calculation - no API key required")
.ExcludeFromDescription();

app.MapPost("/api/demo/image", async (ImageRequest request, HdChartApiClient chartApi) =>
{
    var imageData = await chartApi.GenerateImageAsync(request);
    return imageData != null 
        ? Results.File(imageData, "image/png", "bodygraph-demo.png")
        : Results.Problem("Failed to generate demo image");
})
.WithTags("Demo")
.ExcludeFromDescription();

// ═══════════════════════════════════════════════════════════════════════════════════════
// SELF-SERVICE SIGNUP
// ═══════════════════════════════════════════════════════════════════════════════════════

app.MapPost("/api/signup", (HttpContext ctx, ApiKeyService keyService) =>
{
    var name = ctx.Request.Query["name"].FirstOrDefault() ?? "";
    var email = ctx.Request.Query["email"].FirstOrDefault() ?? "";
    
    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        return Results.BadRequest(new { error = "Name and email are required" });
        
    if (!IsValidEmail(email))
        return Results.BadRequest(new { error = "Invalid email format" });
    
    var apiKey = keyService.CreateKey(name, email, "free");
    return Results.Ok(apiKey);
})
.WithTags("Signup")
.WithDescription("Get a free API key - no credit card required")
.ExcludeFromDescription();

// ═══════════════════════════════════════════════════════════════════════════════════════
// ADMIN ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════════════

app.MapPost("/api/admin/keys", (HttpContext ctx, ApiKeyService keyService) =>
{
    if (!IsAuthorizedAdmin(ctx, adminSecret)) return Results.Unauthorized();
    
    var name = ctx.Request.Query["name"].FirstOrDefault() ?? "unnamed";
    var email = ctx.Request.Query["email"].FirstOrDefault() ?? "";
    var tier = ctx.Request.Query["tier"].FirstOrDefault() ?? "free";
    
    return Results.Ok(keyService.CreateKey(name, email, tier));
})
.WithTags("Admin")
.ExcludeFromDescription();

app.MapGet("/api/admin/keys", (HttpContext ctx, ApiKeyService keyService) =>
{
    if (!IsAuthorizedAdmin(ctx, adminSecret)) return Results.Unauthorized();
    return Results.Ok(keyService.ListKeys());
})
.WithTags("Admin")
.ExcludeFromDescription();

app.MapGet("/api/admin/usage/{apiKey}", (string apiKey, HttpContext ctx, ApiKeyService keyService) =>
{
    if (!IsAuthorizedAdmin(ctx, adminSecret)) return Results.Unauthorized();
    return Results.Ok(keyService.GetUsage(apiKey));
})
.WithTags("Admin")
.ExcludeFromDescription();

app.Run();

// Helper functions
static bool IsAuthorizedAdmin(HttpContext ctx, string adminSecret) =>
    ctx.Request.Headers["X-Admin-Secret"].FirstOrDefault() == adminSecret;

static bool IsValidEmail(string email) =>
    !string.IsNullOrWhiteSpace(email) && email.Contains('@') && email.Contains('.');