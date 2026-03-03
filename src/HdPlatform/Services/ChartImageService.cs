using System.Text.Json;

namespace HdPlatform.Services;

public class ChartImageService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChartImageService> _logger;
    private const string HD_CHART_API_BASE = "http://100.101.12.75:5100";

    public ChartImageService(HttpClient httpClient, ILogger<ChartImageService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<byte[]?> GenerateChartImageAsync(
        DateTime birthDateTime, 
        double latitude, 
        double longitude, 
        string? timezone = null,
        int width = 800,
        int height = 600)
    {
        try
        {
            var request = new
            {
                DateTime = birthDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                Latitude = latitude,
                Longitude = longitude,
                Timezone = timezone ?? "UTC"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation($"Requesting chart image from {HD_CHART_API_BASE}/api/chart/image");
            
            var response = await _httpClient.PostAsync($"{HD_CHART_API_BASE}/api/chart/image", content);
            
            if (response.IsSuccessStatusCode)
            {
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation($"Chart image generated successfully, {imageBytes.Length} bytes");
                return imageBytes;
            }

            _logger.LogWarning($"HD Chart API returned {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chart image");
            return null;
        }
    }

    public async Task<byte[]?> GenerateTransitImageAsync(
        DateTime birthDateTime,
        double birthLatitude,
        double birthLongitude,
        DateTime transitDateTime,
        double transitLatitude,
        double transitLongitude,
        string? timezone = null)
    {
        try
        {
            var request = new
            {
                BirthDateTime = birthDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                BirthLatitude = birthLatitude,
                BirthLongitude = birthLongitude,
                TransitDateTime = transitDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                TransitLatitude = transitLatitude,
                TransitLongitude = transitLongitude,
                Timezone = timezone ?? "UTC"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{HD_CHART_API_BASE}/api/chart/transit", content);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            _logger.LogWarning($"Transit chart API returned {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating transit chart image");
            return null;
        }
    }
}