using HdChartApi.Models;
using HdChartApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddSingleton<HumanDesignService>();
builder.Services.AddSingleton<GeocodingService>();
builder.Services.AddSingleton<ChartImageService>();

var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.Urls.Add("http://127.0.0.1:5100");
app.Urls.Add("http://100.101.12.75:5100");

app.MapGet("/api", () => "Human Design Chart API v1");

// Direct UTC endpoint - no geocoding needed
app.MapPost("/api/chart/utc", (ChartRequest req, HumanDesignService hd) =>
{
    try
    {
        var utcDate = DateTime.SpecifyKind(DateTime.Parse(req.BirthDate), DateTimeKind.Utc);
        var result = hd.CalculateChart(utcDate);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ERROR] /api/chart/utc: {ex}");
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/chart", async (ChartRequest req, HumanDesignService hd, GeocodingService geo) =>
{
    try
    {
        var birthDate = DateTime.Parse(req.BirthDate);
        var geoResult = await geo.GeocodeAsync(req.BirthPlace);
        var utcBirth = geo.ConvertToUtc(birthDate, geoResult.TimeZone);
        var result = hd.CalculateChart(utcBirth);
        return Results.Ok(new { chart = result, location = new { geoResult.Lat, geoResult.Lng, geoResult.TimeZoneId }, utcBirthDate = utcBirth.ToString("o") });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ERROR] /api/chart: {ex}");
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/chart/transit", async (TransitRequest req, HumanDesignService hd, GeocodingService geo) =>
{
    try
    {
        var birthDate = DateTime.Parse(req.BirthDate);
        var geoResult = await geo.GeocodeAsync(req.BirthPlace);
        var utcBirth = geo.ConvertToUtc(birthDate, geoResult.TimeZone);
        var transitDate = DateTime.Parse(req.TransitDate);
        var utcTransit = transitDate.Kind == DateTimeKind.Utc ? transitDate : geo.ConvertToUtc(transitDate, geoResult.TimeZone);
        var result = hd.CalculateTransit(utcBirth, utcTransit);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ERROR] /api/chart/transit: {ex}");
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/chart/image", async (ImageRequest req, HumanDesignService hd, GeocodingService geo, ChartImageService img) =>
{
    try
    {
        var birthDate = DateTime.Parse(req.BirthDate);
        var geoResult = await geo.GeocodeAsync(req.BirthPlace);
        var utcBirth = geo.ConvertToUtc(birthDate, geoResult.TimeZone);
        var chart = hd.GetRawChart(utcBirth);

        byte[] png;
        if (!string.IsNullOrEmpty(req.TransitDate))
        {
            var transitDate = DateTime.Parse(req.TransitDate);
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
        Console.Error.WriteLine($"[ERROR] /api/chart/image: {ex}");
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/chart/composite", async (CompositeRequest req, HumanDesignService hd, GeocodingService geo) =>
{
    try
    {
        var date1 = DateTime.Parse(req.BirthDate1);
        var geo1 = await geo.GeocodeAsync(req.BirthPlace1);
        var utc1 = geo.ConvertToUtc(date1, geo1.TimeZone);
        
        var date2 = DateTime.Parse(req.BirthDate2);
        var geo2 = await geo.GeocodeAsync(req.BirthPlace2);
        var utc2 = geo.ConvertToUtc(date2, geo2.TimeZone);
        
        var result = hd.CalculateComposite(utc1, utc2);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ERROR] /api/chart/composite: {ex}");
        return Results.Problem(ex.Message);
    }
});

app.Run();
