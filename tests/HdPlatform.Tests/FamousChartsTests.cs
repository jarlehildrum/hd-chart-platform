using HdPlatform.Services;

namespace HdPlatform.Tests;

/// <summary>
/// Golden-data tests for HumanDesignService, using birth data from
/// tests/the-famous-rave-collection.mmi (a well-known "famous people" rave
/// chart export) cross-checked against publicly published Human Design
/// readings for each person. Type/Profile/Authority are the fields most
/// consistently documented across independent HD sources, so those are
/// what's asserted here.
/// </summary>
public class FamousChartsTests
{
    private readonly HumanDesignService _hd = new();

    [Theory]
    [InlineData("Barack Obama", "1961-08-05T05:24:00Z", "Projector", "6/2", "Emotional")]
    [InlineData("Michael Jackson", "1958-08-30T00:33:00Z", "Projector", "1/3", "Emotional")]
    [InlineData("Diana, Princess of Wales", "1961-07-01T18:45:00Z", "Projector", "1/3", "Emotional")]
    [InlineData("Elvis Presley", "1935-01-08T10:34:00Z", "Generator", "3/5", "Emotional")]
    [InlineData("Bill Gates", "1955-10-29T06:00:00Z", "Generator", "4/6", "Sacral")]
    public void CalculateChart_MatchesPublishedReading(
        string person, string birthUtc, string expectedType, string expectedProfile, string expectedAuthority)
    {
        var birthDate = DateTime.Parse(birthUtc).ToUniversalTime();

        var chart = _hd.CalculateChart(birthDate);

        Assert.True(expectedType == chart.Type, $"{person}: expected type {expectedType}, got {chart.Type}");
        Assert.True(expectedProfile == chart.Profile, $"{person}: expected profile {expectedProfile}, got {chart.Profile}");
        Assert.True(expectedAuthority == chart.Authority, $"{person}: expected authority {expectedAuthority}, got {chart.Authority}");
    }

    // Steve Jobs' published birth time varies by a few minutes across sources
    // (a well-documented case where a ~3 minute shift flips the profile), so
    // this only pins down Type/Authority, which are stable across those sources.
    [Fact]
    public void CalculateChart_SteveJobs_TypeAndAuthorityMatchPublishedReading()
    {
        var birthDate = DateTime.Parse("1955-02-25T03:15:00Z").ToUniversalTime();

        var chart = _hd.CalculateChart(birthDate);

        Assert.Equal("Generator", chart.Type);
        Assert.Equal("Emotional", chart.Authority);
    }
}
