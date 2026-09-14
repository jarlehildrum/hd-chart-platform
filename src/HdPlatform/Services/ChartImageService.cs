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
        string birthPlace)
    {
        try
        {
            var request = new
            {
                birthDate = birthDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                birthPlace = birthPlace
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
}
