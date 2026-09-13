using HdPlatform.Services;

namespace HdPlatform.Tests;

/// <summary>
/// Golden-data tests for HumanDesignService, using birth data from
/// tests/the-famous-rave-collection.mmi (a well-known "famous people" rave
/// chart export) cross-checked against publicly published Human Design
/// readings for each person.
///
/// Fixtures are split by how much of a chart's reading could be confirmed
/// against independent HD sources: some sources disagree with each other
/// (disputed/uncertain birth times, or sites that lump ManifestingGenerator
/// under the broader "Generator" label), so only the fields that were
/// actually corroborated are asserted for each person.
/// </summary>
public class FamousChartsTests
{
    private readonly HumanDesignService _hd = new();

    private HdPlatform.Models.ChartResponse Calculate(string birthUtc) =>
        _hd.CalculateChart(DateTime.Parse(birthUtc).ToUniversalTime());

    // Type + Profile + Authority all independently confirmed against a published HD reading.
    [Theory]
    [InlineData("Barack Obama", "1961-08-05T05:24:00Z", "Projector", "6/2", "Emotional")]
    [InlineData("Michael Jackson", "1958-08-30T00:33:00Z", "Projector", "1/3", "Emotional")]
    [InlineData("Diana, Princess of Wales", "1961-07-01T18:45:00Z", "Projector", "1/3", "Emotional")]
    [InlineData("Elvis Presley", "1935-01-08T10:34:00Z", "Generator", "3/5", "Emotional")]
    [InlineData("Bill Gates", "1955-10-29T06:00:00Z", "Generator", "4/6", "Sacral")]
    [InlineData("Mick Jagger", "1943-07-26T00:30:00Z", "Projector", "1/3", "Self")]
    [InlineData("Matt Damon", "1970-10-08T19:22:00Z", "Generator", "6/3", "Emotional")]
    [InlineData("Harrison Ford", "1942-07-13T16:41:00Z", "ManifestingGenerator", "6/3", "Emotional")]
    [InlineData("Julian Assange", "1971-07-03T05:00:00Z", "ManifestingGenerator", "2/4", "Emotional")]
    [InlineData("Roger Federer", "1981-08-08T06:40:00Z", "ManifestingGenerator", "3/5", "Sacral")]
    [InlineData("Rafael Nadal", "1986-06-03T17:15:00Z", "Manifestor", "2/4", "Emotional")]
    [InlineData("Salvador Dali", "1904-05-11T08:45:00Z", "Projector", "2/4", "Emotional")]
    [InlineData("Fidel Castro", "1926-08-13T07:00:00Z", "Projector", "1/4", "Spleen")]
    [InlineData("Andy Warhol", "1928-08-06T10:30:00Z", "Generator", "1/3", "Sacral")]
    [InlineData("Marlon Brando", "1924-04-04T05:00:00Z", "Generator", "5/2", "Emotional")]
    [InlineData("Leonardo DiCaprio", "1974-11-11T10:47:00Z", "Projector", "6/2", "Emotional")]
    [InlineData("Amy Winehouse", "1983-09-14T21:25:00Z", "ManifestingGenerator", "5/1", "Emotional")]
    [InlineData("Robert Downey Jr.", "1965-04-04T18:10:00Z", "Generator", "6/2", "Sacral")]
    [InlineData("Adele", "1988-05-05T02:02:00Z", "Manifestor", "2/4", "Emotional")]
    [InlineData("Vladimir Putin", "1952-10-07T06:30:00Z", "Manifestor", "5/1", "Spleen")]
    [InlineData("Tom Hanks", "1956-07-09T18:17:00Z", "ManifestingGenerator", "3/5", "Emotional")]
    [InlineData("Timothy Leary", "1920-10-22T10:45:00Z", "Generator", "3/5", "Sacral")]
    [InlineData("Sting", "1951-10-02T00:30:00Z", "ManifestingGenerator", "5/1", "Emotional")]
    [InlineData("Steven Spielberg", "1946-12-18T23:16:00Z", "Projector", "5/1", "Self")]
    [InlineData("Stephen King", "1947-09-21T05:30:00Z", "ManifestingGenerator", "6/2", "Sacral")]
    [InlineData("Shaquille O'Neal", "1972-03-06T13:00:00Z", "ManifestingGenerator", "5/2", "Sacral")]
    [InlineData("Roman Polanski", "1933-08-18T09:30:00Z", "Generator", "1/3", "Emotional")]
    [InlineData("Richard Branson", "1950-07-18T06:00:00Z", "Generator", "5/1", "Emotional")]
    [InlineData("Queen Elizabeth II", "1926-04-21T01:40:00Z", "Projector", "5/1", "Emotional")]
    [InlineData("Pope John Paul II", "1920-05-18T15:30:00Z", "ManifestingGenerator", "4/6", "Emotional")]
    [InlineData("Naomi Campbell", "1970-05-22T00:00:00Z", "ManifestingGenerator", "1/3", "Emotional")]
    [InlineData("John Travolta", "1954-02-18T19:53:00Z", "ManifestingGenerator", "6/2", "Emotional")]
    [InlineData("Johnny Depp", "1963-06-09T14:44:00Z", "Manifestor", "2/4", "Emotional")]
    [InlineData("John F. Kennedy", "1917-05-29T15:00:00Z", "Projector", "3/5", "Emotional")]
    [InlineData("Jane Fonda", "1937-12-21T14:14:00Z", "Generator", "2/4", "Sacral")]
    [InlineData("Hillary Clinton", "1947-10-26T14:00:00Z", "ManifestingGenerator", "1/3", "Sacral")]
    [InlineData("George Lucas", "1944-05-14T12:40:00Z", "ManifestingGenerator", "6/2", "Emotional")]
    [InlineData("Drew Barrymore", "1975-02-22T19:51:00Z", "ManifestingGenerator", "4/6", "Emotional")]
    [InlineData("Christopher Reeve", "1952-09-25T07:12:00Z", "Projector", "5/1", "Spleen")]
    [InlineData("Carl Gustav Jung", "1875-07-26T19:19:00Z", "Generator", "2/4", "Emotional")]
    [InlineData("Britney Spears", "1981-12-02T07:30:00Z", "ManifestingGenerator", "5/1", "Sacral")]
    [InlineData("Bill Clinton", "1946-08-19T14:51:00Z", "Generator", "2/4", "Sacral")]
    [InlineData("Ben Affleck", "1972-08-15T09:52:00Z", "ManifestingGenerator", "5/1", "Sacral")]
    [InlineData("Barbra Streisand", "1942-04-24T09:08:00Z", "Projector", "2/4", "Self")]
    [InlineData("Anthony Hopkins", "1937-12-31T09:15:00Z", "Generator", "6/2", "Sacral")]
    [InlineData("Angelina Jolie", "1975-06-04T16:09:00Z", "ManifestingGenerator", "3/5", "Emotional")]
    [InlineData("Al Gore", "1948-03-31T12:52:00Z", "Manifestor", "2/4", "Heart")]
    [InlineData("H.G. Wells", "1866-09-21T16:29:52Z", "Reflector", "1/3", "Outer")]
    [InlineData("Chelsea Clinton", "1980-02-28T05:24:00Z", "Generator", "4/6", "Emotional")]
    [InlineData("Sophia Loren", "1934-09-20T13:10:00Z", "Manifestor", "5/1", "Emotional")]
    [InlineData("Luciano Pavarotti", "1935-10-12T00:40:00Z", "Generator", "3/5", "Emotional")]
    // Jim Morrison's published birth time is itself noted as uncertain by HD
    // sources, but the reading it produces (MG, Emotional, 5/1) matches ours.
    [InlineData("Jim Morrison", "1943-12-08T15:55:00Z", "ManifestingGenerator", "5/1", "Emotional")]
    public void CalculateChart_MatchesPublishedReading(
        string person, string birthUtc, string expectedType, string expectedProfile, string expectedAuthority)
    {
        var chart = Calculate(birthUtc);

        Assert.True(expectedType == chart.Type, $"{person}: expected type {expectedType}, got {chart.Type}");
        Assert.True(expectedProfile == chart.Profile, $"{person}: expected profile {expectedProfile}, got {chart.Profile}");
        Assert.True(expectedAuthority == chart.Authority, $"{person}: expected authority {expectedAuthority}, got {chart.Authority}");
    }

    // Type + Authority confirmed; Profile is either disputed across sources
    // (e.g. birth-time sensitivity) or wasn't consistently published.
    [Theory]
    [InlineData("Steve Jobs", "1955-02-25T03:15:00Z", "Generator", "Emotional")]
    [InlineData("Marilyn Monroe", "1926-06-01T17:30:00Z", "Projector", "Emotional")]
    [InlineData("George Clooney", "1961-05-06T07:58:00Z", "Projector", "Emotional")]
    [InlineData("Charlie Sheen", "1965-09-04T02:48:00Z", "Projector", "Emotional")]
    public void CalculateChart_TypeAndAuthorityMatchPublishedReading(
        string person, string birthUtc, string expectedType, string expectedAuthority)
    {
        var chart = Calculate(birthUtc);

        Assert.True(expectedType == chart.Type, $"{person}: expected type {expectedType}, got {chart.Type}");
        Assert.True(expectedAuthority == chart.Authority, $"{person}: expected authority {expectedAuthority}, got {chart.Authority}");
    }

    // Type + Profile confirmed; Authority itself is disputed between published sources.
    [Theory]
    [InlineData("Brad Pitt", "1963-12-18T12:31:00Z", "Projector", "4/6")]
    public void CalculateChart_TypeAndProfileMatchPublishedReading(
        string person, string birthUtc, string expectedType, string expectedProfile)
    {
        var chart = Calculate(birthUtc);

        Assert.True(expectedType == chart.Type, $"{person}: expected type {expectedType}, got {chart.Type}");
        Assert.True(expectedProfile == chart.Profile, $"{person}: expected profile {expectedProfile}, got {chart.Profile}");
    }

    // Only Profile could be independently confirmed: some public HD sites
    // don't distinguish Generator from ManifestingGenerator (e.g. Marie
    // Curie, Osama Bin Laden are commonly just labelled "Generator" even
    // when the underlying chart is a ManifestingGenerator), or simply don't
    // publish a Type/Authority for the person at all (Margaret Thatcher).
    [Theory]
    [InlineData("Marie Curie", "1867-11-07T13:30:00Z", "2/4")]
    [InlineData("Margaret Thatcher", "1925-10-13T09:00:00Z", "5/1")]
    [InlineData("Osama Bin Laden", "1957-03-10T07:58:00Z", "3/5")]
    public void CalculateChart_ProfileMatchesPublishedReading(string person, string birthUtc, string expectedProfile)
    {
        var chart = Calculate(birthUtc);

        Assert.True(expectedProfile == chart.Profile, $"{person}: expected profile {expectedProfile}, got {chart.Profile}");
    }

    // Only Type could be independently confirmed (no published profile found, or profile disagreed).
    [Theory]
    [InlineData("Karl Marx", "1818-05-05T07:15:00Z", "Projector")]
    [InlineData("Frida Kahlo", "1907-07-06T15:07:00Z", "Manifestor")]
    public void CalculateChart_TypeMatchesPublishedReading(string person, string birthUtc, string expectedType)
    {
        var chart = Calculate(birthUtc);

        Assert.True(expectedType == chart.Type, $"{person}: expected type {expectedType}, got {chart.Type}");
    }
}
