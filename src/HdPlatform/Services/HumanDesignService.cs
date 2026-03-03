using System.Text.Json;
using HdPlatform.Core.Models;

namespace HdPlatform.Services;

public class HumanDesignService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HumanDesignService> _logger;
    private const string HD_CHART_API_BASE = "http://100.101.12.75:5100";

    public HumanDesignService(HttpClient httpClient, ILogger<HumanDesignService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> GenerateChartAsync(
        DateTime birthDateTime,
        double latitude,
        double longitude,
        string? timezone = null)
    {
        try
        {
            var request = new ChartRequest
            {
                DateTime = birthDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                Latitude = latitude,
                Longitude = longitude,
                Timezone = timezone ?? "UTC"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation($"Requesting chart from {HD_CHART_API_BASE}/api/chart");
            
            var response = await _httpClient.PostAsync($"{HD_CHART_API_BASE}/api/chart", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Chart generated successfully");
                return result;
            }

            _logger.LogWarning($"HD Chart API returned {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chart");
            return null;
        }
    }

    public async Task<string?> GenerateTransitAsync(TransitRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{HD_CHART_API_BASE}/api/chart/transit", content);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            _logger.LogWarning($"Transit chart API returned {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating transit chart");
            return null;
        }
    }
}