using System.Text.Json.Serialization;

namespace HdPlatform.Core.Models;

public class ChartRequest
{
    [JsonPropertyName("dateTime")]
    public string DateTime { get; set; } = "";
    
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
    
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
    
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
}

public class TransitRequest
{
    [JsonPropertyName("birthDateTime")]
    public string BirthDateTime { get; set; } = "";
    
    [JsonPropertyName("birthLatitude")]
    public double BirthLatitude { get; set; }
    
    [JsonPropertyName("birthLongitude")]
    public double BirthLongitude { get; set; }
    
    [JsonPropertyName("transitDateTime")]
    public string TransitDateTime { get; set; } = "";
    
    [JsonPropertyName("transitLatitude")]
    public double TransitLatitude { get; set; }
    
    [JsonPropertyName("transitLongitude")]
    public double TransitLongitude { get; set; }
    
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
}

public class ApiKeyInfo
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Plan { get; set; } = "Free";
    public int MonthlyLimit { get; set; } = 50;
    public int UsageThisMonth { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}