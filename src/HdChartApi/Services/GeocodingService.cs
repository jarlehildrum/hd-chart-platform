using System.Text.Json;
using GeoTimeZone;

namespace HdChartApi.Services;

public class GeocodingService
{
    private static readonly HttpClient _http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "HdChartApi/1.0" } }
    };

    public async Task<GeoResult> GeocodeAsync(string place)
    {
        var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(place)}&format=json&limit=1";
        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement;
        if (arr.GetArrayLength() == 0)
            throw new Exception($"Could not geocode place: {place}");
        var first = arr[0];
        var lat = double.Parse(first.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var lng = double.Parse(first.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        
        // Look up IANA timezone from coordinates
        var tzResult = TimeZoneLookup.GetTimeZone(lat, lng);
        var ianaId = tzResult.Result;
        
        // Convert IANA to .NET TimeZoneInfo
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(ianaId);
        }
        catch
        {
            tz = TimeZoneInfo.Utc;
        }
        
        return new GeoResult(lat, lng, ianaId, tz);
    }

    /// <summary>
    /// Convert a local birth time to UTC using the timezone of the birth place.
    /// </summary>
    public DateTime ConvertToUtc(DateTime localTime, TimeZoneInfo tz)
    {
        // Treat as unspecified local time
        var unspecified = DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
    }
}

public record GeoResult(double Lat, double Lng, string TimeZoneId, TimeZoneInfo TimeZone);
