namespace HdChartApi.Models;

public record ChartRequest(string BirthDate, string BirthPlace);

public record TransitRequest(string BirthDate, string BirthPlace, string TransitDate);

public record ImageRequest(string BirthDate, string BirthPlace, string? TransitDate = null);

public record CompositeRequest(
    string BirthDate1, string BirthPlace1,
    string BirthDate2, string BirthPlace2);
