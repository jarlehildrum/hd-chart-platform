using HdPlatform.Models;
using HdPlatform.Services;
using HdPlatform.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Core Services - now local instead of HTTP client
builder.Services.AddSingleton<HumanDesignService>();
builder.Services.AddSingleton<GeocodingService>();
builder.Services.AddSingleton<ChartImageService>();
builder.Services.AddSingleton<ApiKeyService>();

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
// CHART ENDPOINTS (Require API Key) - Now using local services
// ═══════════════════════════════════════════════════════════════════════════════════════

app.MapPost("/api/chart", async (ChartRequest request, HumanDesignService hd, GeocodingService geo) =>
{
    try
    {
        var birthDate = DateTime.Parse(request.BirthDate);
        var geoResult = await geo.GeocodeAsync(request.BirthPlace);
        var utcBirth = geo.ConvertToUtc(birthDate, geoResult.TimeZone);
        var result = hd.CalculateChart(utcBirth);
        return Results.Ok(new { chart = result, location = new { geoResult.Lat, geoResult.Lng, geoResult.TimeZoneId }, utcBirthDate = utcBirth.ToString("o") });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error calculating chart");
        return Results.Problem(ex.Message);
    }
})
.WithTags("Charts")
.WithDescription("Calculate natal chart with automatic geocoding and timezone conversion")
.Accepts<ChartRequest>("application/json")
.Produces(200)
.ProducesProblem(400)
.ProducesProblem(401)
.ProducesProblem(429);

app.MapPost("/api/chart/utc", (ChartRequest request, HumanDesignService hd) =>
{
    try
    {
        var utcDate = DateTime.SpecifyKind(DateTime.Parse(request.BirthDate), DateTimeKind.Utc);
        var result = hd.CalculateChart(utcDate);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error calculating UTC chart");
        return Results.Problem(ex.Message);
    }
})
.WithTags("Charts")
.WithDescription("Calculate natal chart from UTC birth time (no geocoding needed)")
.Accepts<ChartRequest>("application/json")
.Produces(200);

app.MapPost("/api/chart/transit", async (TransitRequest request, HumanDesignService hd, GeocodingService geo) =>
{
    try
    {
        var birthDate = DateTime.Parse(request.BirthDate);
        var geoResult = await geo.GeocodeAsync(request.BirthPlace);
        var utcBirth = geo.ConvertToUtc(birthDate, geoResult.TimeZone);
        var transitDate = DateTime.Parse(request.TransitDate);
        var utcTransit = transitDate.Kind == DateTimeKind.Utc ? transitDate : geo.ConvertToUtc(transitDate, geoResult.TimeZone);
        var result = hd.CalculateTransit(utcBirth, utcTransit);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error calculating transit");
        return Results.Problem(ex.Message);
    }
})
.WithTags("Charts")
.WithDescription("Calculate natal chart with transit overlay for specific date")
.Accepts<TransitRequest>("application/json")
.Produces(200);

app.MapPost("/api/chart/composite", async (CompositeRequest request, HumanDesignService hd, GeocodingService geo) =>
{
    try
    {
        var date1 = DateTime.Parse(request.BirthDate1);
        var geo1 = await geo.GeocodeAsync(request.BirthPlace1);
        var utc1 = geo.ConvertToUtc(date1, geo1.TimeZone);
        
        var date2 = DateTime.Parse(request.BirthDate2);
        var geo2 = await geo.GeocodeAsync(request.BirthPlace2);
        var utc2 = geo.ConvertToUtc(date2, geo2.TimeZone);
        
        var result = hd.CalculateComposite(utc1, utc2);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error calculating composite");
        return Results.Problem(ex.Message);
    }
})
.WithTags("Charts")
.WithDescription("Calculate composite (relationship) chart between two people")
.Accepts<CompositeRequest>("application/json")
.Produces(200);

app.MapPost("/api/chart/image", async (ImageRequest request, HumanDesignService hd, GeocodingService geo, ChartImageService img) =>
{
    try
    {
        var birthDate = DateTime.Parse(request.BirthDate);
        var geoResult = await geo.GeocodeAsync(request.BirthPlace);
        var utcBirth = geo.ConvertToUtc(birthDate, geoResult.TimeZone);
        var chart = hd.GetRawChart(utcBirth);
        
        byte[] png;
        if (!string.IsNullOrEmpty(request.TransitDate))
        {
            var transitDate = DateTime.Parse(request.TransitDate);
            var utcTransit = geo.ConvertToUtc(transitDate, geoResult.TimeZone);
            var transit = hd.GetRawTransit(utcBirth, utcTransit);
            png = img.GenerateBodygraphPng(chart, transit);
        }
        else
        {
            png = img.GenerateBodygraphPng(chart);
        }
        return Results.File(png, "image/png", "bodygraph.png");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error generating chart image");
        return Results.Problem(ex.Message);
    }
})
.WithTags("Charts")
.WithDescription("Generate professional bodygraph PNG image")
.Accepts<ImageRequest>("application/json")
.Produces(200, contentType: "image/png");

// ═══════════════════════════════════════════════════════════════════════════════════════
// DEMO ENDPOINTS (No Authentication)
// ═══════════════════════════════════════════════════════════════════════════════════════

app.MapPost("/api/demo/chart", async (ChartRequest request, HumanDesignService hd, GeocodingService geo) =>
{
    try
    {
        var birthDate = DateTime.Parse(request.BirthDate);
        var geoResult = await geo.GeocodeAsync(request.BirthPlace);
        var utcBirth = geo.ConvertToUtc(birthDate, geoResult.TimeZone);
        var result = hd.CalculateChart(utcBirth);
        return Results.Ok(new { chart = result, location = new { geoResult.Lat, geoResult.Lng, geoResult.TimeZoneId } });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error in demo chart");
        return Results.Problem(ex.Message);
    }
}).WithTags("Demo").WithDescription("Demo chart calculation - no API key required").ExcludeFromDescription();

app.MapPost("/api/demo/image", async (ImageRequest request, HumanDesignService hd, GeocodingService geo, ChartImageService img) =>
{
    try
    {
        var birthDate = DateTime.Parse(request.BirthDate);
        var geoResult = await geo.GeocodeAsync(request.BirthPlace);
        var utcBirth = geo.ConvertToUtc(birthDate, geoResult.TimeZone);
        var chart = hd.GetRawChart(utcBirth);
        var png = img.GenerateBodygraphPng(chart);
        return Results.File(png, "image/png", "bodygraph-demo.png");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error generating demo image");
        return Results.Problem(ex.Message);
    }
}).WithTags("Demo").ExcludeFromDescription();

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