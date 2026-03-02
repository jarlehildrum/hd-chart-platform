namespace HdPlatform.Models;

public record ChartRequest(string BirthDate, string BirthPlace);
public record TransitRequest(string BirthDate, string BirthPlace, string TransitDate);
public record ImageRequest(string BirthDate, string BirthPlace, string? TransitDate = null);
public record CompositeRequest(string BirthDate1, string BirthPlace1, string BirthDate2, string BirthPlace2);

public record ApiKeyInfo(string Key, string Name, string Email, string Tier, DateTime CreatedAt, bool Active)
{
    public int MonthlyLimit => Tier switch
    {
        "free" => 50, "pro" => 2000, "business" => 100_000, _ => 50
    };
}

public record ApiResponse<T>(bool Success, T? Data, string? Error);