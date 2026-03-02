using HdPlatform.Models;
using System.Text.Json;

namespace HdPlatform.Services;

public class HdChartApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HdChartApiClient> _logger;
    private readonly string _baseUrl;

    public HdChartApiClient(HttpClient httpClient, IConfiguration config, ILogger<HdChartApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = config.GetValue<string>("HdChartApi:BaseUrl") ?? "http://100.101.12.75:5100";
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            _logger.LogInformation("Calling HD Chart API: {Endpoint}", endpoint);
            var response = await _httpClient.PostAsync(endpoint, content);
            
            var responseJson = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("HD Chart API error {StatusCode}: {Response}", response.StatusCode, responseJson);
                return new ApiResponse<T>(false, default, $"HD Chart API error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            
            return new ApiResponse<T>(true, result, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling HD Chart API");
            return new ApiResponse<T>(false, default, "Network error connecting to chart service");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling HD Chart API");
            return new ApiResponse<T>(false, default, "Chart calculation timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling HD Chart API");
            return new ApiResponse<T>(false, default, "Internal service error");
        }
    }

    public async Task<ApiResponse<object>> CalculateChartAsync(ChartRequest request)
        => await PostAsync<object>("/api/chart", request);

    public async Task<ApiResponse<object>> CalculateUtcChartAsync(ChartRequest request)
        => await PostAsync<object>("/api/chart/utc", request);

    public async Task<ApiResponse<object>> CalculateTransitAsync(TransitRequest request)
        => await PostAsync<object>("/api/chart/transit", request);

    public async Task<ApiResponse<object>> CalculateCompositeAsync(CompositeRequest request)
        => await PostAsync<object>("/api/chart/composite", request);

    public async Task<byte[]?> GenerateImageAsync(ImageRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/chart/image", content);
            
            if (!response.IsSuccessStatusCode)
                return null;
            
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chart image");
            return null;
        }
    }
}