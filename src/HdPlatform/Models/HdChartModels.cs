namespace HdPlatform.Models;

public record ChartResponse(
    string Type,
    string Profile,
    string Strategy,
    string Authority,
    string SplitDefinition,
    string IncarnationCross,
    List<string> ActiveChannels,
    List<ActivationInfo> PersonalityActivation,
    List<ActivationInfo> DesignActivation,
    VariablesInfo Variables,
    Dictionary<string, bool> Centers);

public record ActivationInfo(
    string Planet,
    string Gate,
    int Line,
    string FixingState);

public record VariablesInfo(
    VariableInfo Digestion,
    VariableInfo Perspective,
    VariableInfo Environment,
    VariableInfo Awareness);

public record VariableInfo(
    string Orientation,
    int Color,
    int Tone,
    int Base);

public record TransitResponse(
    ChartResponse NatalChart,
    List<ActivationInfo> TransitActivation,
    Dictionary<string, string> ChannelActivations);

public record CompositeResponse(
    Dictionary<string, string> ChannelActivations,
    List<string> ActiveGates,
    string SplitDefinition);