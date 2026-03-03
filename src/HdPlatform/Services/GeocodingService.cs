using System.Text.Json;

namespace HdPlatform.Services;

public class GeocodingService
{
    private static readonly HttpClient _http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "HdChartApi/1.0" } }
    };

    public async Task<GeoResult> GeocodeAsync(string place)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(place)}&format=json&limit=1";
            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var arr = doc.RootElement;
            if (arr.GetArrayLength() == 0)
                throw new Exception($"Could not geocode place: {place}");

            var item = arr[0];
            var lat = double.Parse(item.GetProperty("lat").GetString()!);
            var lon = double.Parse(item.GetProperty("lon").GetString()!);
            var displayName = item.GetProperty("display_name").GetString()!;

            // Simple timezone estimation (fallback to UTC)
            var timezone = "UTC";
            
            return new GeoResult
            {
                Latitude = lat,
                Longitude = lon,
                DisplayName = displayName,
                Timezone = timezone
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Geocoding failed for '{place}': {ex.Message}");
        }
    }
}

public class GeoResult
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string DisplayName { get; set; } = "";
    public string Timezone { get; set; } = "UTC";
}