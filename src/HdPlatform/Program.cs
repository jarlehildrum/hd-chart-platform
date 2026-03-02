using HdPlatform.Models;
using HdPlatform.Services;
using HdPlatform.Middleware;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    Environment.GetEnvironmentVariable("DATABASE_URL") ??
    "Host=localhost;Port=5432;Database=hdplatform;Username=hduser;Password=hdplatform123";

builder.Services.AddDbContext<HdPlatformContext>(options =>
    options.UseNpgsql(connectionString));

// Core Services - now using database instead of JSON
builder.Services.AddScoped<DatabaseApiKeyService>();
builder.Services.AddScoped<StripeService>();
builder.Services.AddSingleton<HumanDesignService>();
builder.Services.AddSingleton<GeocodingService>();
builder.Services.AddSingleton<ChartImageService>();

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
        Description = "Professional Human Design chart calculations with Stripe billing. Built by a certified BG5 consultant.",
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

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HdPlatformContext>();
    try
    {
        await context.Database.MigrateAsync();
        app.Logger.LogInformation("Database migration completed");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database migration failed");
    }
}

// Middleware pipeline
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HD Chart API v1");
    c.RoutePrefix = "docs";
});
app.UseMiddleware<DatabaseApiKeyMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();

var adminSecret = builder.Configuration.GetValue<string>("AdminSecret") ?? Environment.GetEnvironmentVariable("HD_ADMIN_SECRET") ?? "changeme";

// ═══════════════════════════════════════════════════════════════════════════════════════
// PUBLIC ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════════════

app.MapGet("/api", () => Results.Ok(new
{
    name = "Human Design Chart API",
    version = "v2.0",
    description = "Professional HD chart calculations with Stripe billing",
    docs = "/docs",
    status = "operational",
    built_by = "Certified BG5 consultant",
    billing_enabled = true
})).WithTags("Info").WithDescription("API information and status");

app.MapGet("/api/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    uptime = Environment.TickCount64 / 1000.0,
    database = "postgresql",
    billing = "stripe"
})).WithTags("Info").WithDescription("Health check endpoint");

// ═══════════════════════════════════════════════════════════════════════════════════════
// STRIPE BILLING ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════════════

app.MapPost("/api/checkout", async (CheckoutRequest request, StripeService stripeService) =>
{
    try
    {
        var result = await stripeService.CreateCheckoutSessionAsync(request);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error creating checkout session");
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithTags("Billing")
.WithDescription("Create Stripe checkout session for Pro or Business plan")
.Accepts<CheckoutRequest>("application/json")
.Produces<CheckoutResponse>(200)
.ProducesProblem(400);

app.MapPost("/api/billing-portal", async (BillingPortalRequest request, StripeService stripeService) =>
{
    try
    {
        var result = await stripeService.CreateBillingPortalAsync(request);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error creating billing portal");
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithTags("Billing")
.WithDescription("Create Stripe customer billing portal")
.Accepts<BillingPortalRequest>("application/json")
.Produces<BillingPortalResponse>(200)
.ProducesProblem(400);

app.MapPost("/api/webhooks/stripe", async (HttpContext context, StripeService stripeService) =>
{
    try
    {
        var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var signature = context.Request.Headers["Stripe-Signature"].FirstOrDefault() ?? "";

        await stripeService.HandleStripeWebhookAsync(json, signature);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error processing Stripe webhook");
        return Results.BadRequest(new { error = "Webhook processing failed" });
    }
})
.WithTags("Webhooks")
.WithDescription("Stripe webhook endpoint for subscription events")
.ExcludeFromDescription();

// ═══════════════════════════════════════════════════════════════════════════════════════
// CHART ENDPOINTS (Require API Key)
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

app.MapPost("/api/signup", async (HttpContext ctx, DatabaseApiKeyService keyService) =>
{
    var name = ctx.Request.Query["name"].FirstOrDefault() ?? "";
    var email = ctx.Request.Query["email"].FirstOrDefault() ?? "";
    
    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        return Results.BadRequest(new { error = "Name and email are required" });
        
    if (!IsValidEmail(email))
        return Results.BadRequest(new { error = "Invalid email format" });
    
    try
    {
        var apiKey = await keyService.CreateKeyAsync(name, email, "free");
        return Results.Ok(new
        {
            key = apiKey.Key,
            name = apiKey.Name,
            email = apiKey.Email,
            tier = apiKey.Tier,
            monthlyLimit = apiKey.MonthlyLimit,
            createdAt = apiKey.CreatedAt,
            active = apiKey.Active
        });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error creating API key");
        return Results.Problem("Failed to create API key");
    }
})
.WithTags("Signup")
.WithDescription("Get a free API key - no credit card required")
.ExcludeFromDescription();

// ═══════════════════════════════════════════════════════════════════════════════════════
// ADMIN ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════════════

app.MapPost("/api/admin/keys", async (HttpContext ctx, DatabaseApiKeyService keyService) =>
{
    if (!IsAuthorizedAdmin(ctx, adminSecret)) return Results.Unauthorized();
    
    var name = ctx.Request.Query["name"].FirstOrDefault() ?? "unnamed";
    var email = ctx.Request.Query["email"].FirstOrDefault() ?? "";
    var tier = ctx.Request.Query["tier"].FirstOrDefault() ?? "free";
    
    try
    {
        var apiKey = await keyService.CreateKeyAsync(name, email, tier);
        return Results.Ok(apiKey);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error creating admin API key");
        return Results.Problem("Failed to create API key");
    }
})
.WithTags("Admin")
.ExcludeFromDescription();

app.MapGet("/api/admin/keys", async (HttpContext ctx, DatabaseApiKeyService keyService) =>
{
    if (!IsAuthorizedAdmin(ctx, adminSecret)) return Results.Unauthorized();
    
    try
    {
        var keys = await keyService.ListKeysAsync();
        return Results.Ok(keys);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error listing API keys");
        return Results.Problem("Failed to list API keys");
    }
})
.WithTags("Admin")
.ExcludeFromDescription();

app.MapGet("/api/admin/usage/{apiKey}", async (string apiKey, HttpContext ctx, DatabaseApiKeyService keyService) =>
{
    if (!IsAuthorizedAdmin(ctx, adminSecret)) return Results.Unauthorized();
    
    try
    {
        var usage = await keyService.GetUsageAsync(apiKey);
        return Results.Ok(usage);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error getting API usage");
        return Results.Problem("Failed to get usage data");
    }
})
.WithTags("Admin")
.ExcludeFromDescription();

app.MapGet("/api/admin/analytics", async (HttpContext ctx, DatabaseApiKeyService keyService) =>
{
    if (!IsAuthorizedAdmin(ctx, adminSecret)) return Results.Unauthorized();
    
    try
    {
        var analytics = await keyService.GetAnalyticsAsync();
        return Results.Ok(analytics);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error getting analytics");
        return Results.Problem("Failed to get analytics data");
    }
})
.WithTags("Admin")
.WithDescription("Get platform analytics and metrics")
.ExcludeFromDescription();

app.Run();

// Helper functions
static bool IsAuthorizedAdmin(HttpContext ctx, string adminSecret) =>
    ctx.Request.Headers["X-Admin-Secret"].FirstOrDefault() == adminSecret;

static bool IsValidEmail(string email) =>
    !string.IsNullOrWhiteSpace(email) && email.Contains('@') && email.Contains('.');