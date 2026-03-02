using SharpAstrology.DataModels;
using SharpAstrology.Enums;
using SharpAstrology.Ephemerides;
using SharpAstrology.Interfaces;
using HdChartApi.Models;

namespace HdChartApi.Services;

public class HumanDesignService
{
    private readonly SwissEphemeridesService _ephService;
    private readonly object _lock = new();

    public HumanDesignService()
    {
        const string ephePath = "/home/jarle/projects/hd-chart-api/ephe";
        Console.WriteLine($"[HD] Ephemeris path: {ephePath}, exists: {Directory.Exists(ephePath)}");
        if (Directory.Exists(ephePath))
            foreach (var f in Directory.GetFiles(ephePath)) Console.WriteLine($"[HD]   {Path.GetFileName(f)}");
        _ephService = new SwissEphemeridesService(ephePath, EphType.Swiss);
        Console.WriteLine("[HD] SwissEphemeridesService created OK");
    }

    public ChartResponse CalculateChart(DateTime birthDateUtc)
    {
        lock (_lock)
        {
            using var eph = _ephService.CreateContext();
            var chart = new HumanDesignChart(birthDateUtc, eph, EphCalculationMode.Tropic);
            return MapChart(chart);
        }
    }

    public TransitResponse CalculateTransit(DateTime birthDateUtc, DateTime transitDateUtc)
    {
        lock (_lock)
        {
            using var eph = _ephService.CreateContext();
            var chart = new HumanDesignChart(birthDateUtc, eph, EphCalculationMode.Tropic);
            var transit = new HumanDesignTransitChart(birthDateUtc, transitDateUtc, eph, EphCalculationMode.Tropic);

            var natalResponse = MapChart(chart);

            var transitActivations = transit.TransitActivation
                .Select(kv => MapActivation(kv.Key, kv.Value, transit.TransitFixation.GetValueOrDefault(kv.Key)))
                .ToList();

            var channelActs = transit.ChannelActivations
                .ToDictionary(kv => FormatChannel(kv.Key), kv => kv.Value.ToString());

            return new TransitResponse(natalResponse, transitActivations, channelActs);
        }
    }

    public CompositeResponse CalculateComposite(DateTime date1Utc, DateTime date2Utc)
    {
        lock (_lock)
        {
            using var eph = _ephService.CreateContext();
            var composite = new HumanDesignCompositeChart(date1Utc, date2Utc, eph, EphCalculationMode.Tropic);

            var channelActs = composite.ChannelActivations
                .ToDictionary(kv => FormatChannel(kv.Key), kv => kv.Value.ToString());

            return new CompositeResponse(
                channelActs,
                composite.ActiveGates.Select(g => FormatGate(g)).ToList(),
                composite.SplitDefinition.ToString());
        }
    }

    public HumanDesignChart GetRawChart(DateTime birthDateUtc)
    {
        lock (_lock)
        {
            using var eph = _ephService.CreateContext();
            return new HumanDesignChart(birthDateUtc, eph, EphCalculationMode.Tropic);
        }
    }

    public HumanDesignTransitChart GetRawTransit(DateTime birthDateUtc, DateTime transitDateUtc)
    {
        lock (_lock)
        {
            using var eph = _ephService.CreateContext();
            return new HumanDesignTransitChart(birthDateUtc, transitDateUtc, eph, EphCalculationMode.Tropic);
        }
    }

    private ChartResponse MapChart(HumanDesignChart chart)
    {
        var personalityActs = chart.PersonalityActivation
            .Select(kv => MapActivation(kv.Key, kv.Value, chart.PersonalityFixation.GetValueOrDefault(kv.Key)))
            .ToList();

        var designActs = chart.DesignActivation
            .Select(kv => MapActivation(kv.Key, kv.Value, chart.DesignFixation.GetValueOrDefault(kv.Key)))
            .ToList();

        var centers = new Dictionary<string, bool>();
        foreach (Centers c in Enum.GetValues<Centers>())
        {
            var act = chart.CenterActivations.GetValueOrDefault(c);
            centers[c.ToString()] = act != ActivationTypes.None;
        }

        var strategy = chart.Type switch
        {
            Types.Manifestor => "To Inform",
            Types.Generator => "Wait To Respond",
            Types.ManifestingGenerator => "Wait To Respond",
            Types.Projector => "Wait For The Invitation",
            Types.Reflector => "Wait A Lunar Cycle",
            _ => chart.Type.ToString()
        };

        return new ChartResponse(
            Type: chart.Type.ToString(),
            Profile: FormatProfile(chart.Profile),
            Strategy: strategy,
            Authority: chart.Strategy.ToString(),
            SplitDefinition: chart.SplitDefinition.ToString(),
            IncarnationCross: FormatIncarnationCross(chart.IncarnationCross),
            ActiveChannels: chart.ActiveChannels.Select(FormatChannel).ToList(),
            PersonalityActivation: personalityActs,
            DesignActivation: designActs,
            Variables: MapVariables(chart.Variables),
            Centers: centers
        );
    }

    private static ActivationInfo MapActivation(Planets planet, Activation activation, PlanetaryFixation? fixation)
    {
        return new ActivationInfo(
            Planet: planet.ToString(),
            Gate: FormatGate(activation.Gate),
            Line: (int)activation.Line,
            FixingState: fixation?.FixingState.ToString() ?? "None"
        );
    }

    private static VariablesInfo MapVariables(Variables v) => new(
        MapVariable(v.Digestion),
        MapVariable(v.Perspective),
        MapVariable(v.Environment),
        MapVariable(v.Awareness));

    private static VariableInfo MapVariable(Variable v) => new(
        v.Orientation.ToString(),
        (int)v.Color,
        (int)v.Tone,
        (int)v.Base);

    private static string FormatGate(Gates gate)
    {
        return gate.ToString().Replace("Key", "");
    }

    private static string FormatChannel(Channels channel)
    {
        var name = channel.ToString();
        // e.g. Key1Key8 -> 1-8
        var parts = name.Split("Key", StringSplitOptions.RemoveEmptyEntries);
        return string.Join("-", parts);
    }

    private static string FormatProfile(Profiles profile) => profile switch
    {
        Profiles.OneThree => "1/3",
        Profiles.OneFour => "1/4",
        Profiles.TwoFour => "2/4",
        Profiles.TwoFive => "2/5",
        Profiles.ThreeFive => "3/5",
        Profiles.ThreeSix => "3/6",
        Profiles.FourSix => "4/6",
        Profiles.FourOne => "4/1",
        Profiles.FiveOne => "5/1",
        Profiles.FiveTwo => "5/2",
        Profiles.SixTwo => "6/2",
        Profiles.SixThree => "6/3",
        _ => profile.ToString()
    };

    private static string FormatIncarnationCross(IncarnationCrosses cross)
    {
        var name = cross.ToString();
        // Convert PascalCase to spaces
        var result = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        result = System.Text.RegularExpressions.Regex.Replace(result, "([A-Z]+)([A-Z][a-z])", "$1 $2");
        return result;
    }
}
