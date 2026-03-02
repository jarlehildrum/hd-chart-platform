using SharpAstrology.DataModels;
using SharpAstrology.Enums;
using SkiaSharp;
using Svg.Skia;
using System.Globalization;
using System.Text;

namespace HdChartApi.Services;

public class ChartImageService
{
    private const string PersonalityColor = "#1a1a1a";
    private const string DesignColor = "#cc3366";
    private const string TransitColor = "#6644cc";
    private const string InactiveColor = "#e0ddd8";
    private const string BgColor = "#FAFAF5";
    private const string FontFamily = "'Segoe UI', Arial, sans-serif";

    private static readonly Dictionary<Centers, string> CenterColorMap = new()
    {
        [Centers.Root] = "#E88835",
        [Centers.Sacral] = "#FE352C",
        [Centers.Emotions] = "#E88835",
        [Centers.Spleen] = "#E88835",
        [Centers.Heart] = "#FE352C",
        [Centers.Self] = "#FFD12B",
        [Centers.Throat] = "#E88835",
        [Centers.Mind] = "#87FE49",
        [Centers.Crown] = "#FFD12B"
    };

    private static readonly Dictionary<Centers, string> CenterGradientLight = new()
    {
        [Centers.Root] = "#F0A050",
        [Centers.Sacral] = "#FF6655",
        [Centers.Emotions] = "#F0A050",
        [Centers.Spleen] = "#F0A050",
        [Centers.Heart] = "#FF6655",
        [Centers.Self] = "#FFE066",
        [Centers.Throat] = "#F0A050",
        [Centers.Mind] = "#AAFE77",
        [Centers.Crown] = "#FFE066"
    };

    // HD standard planet order
    private static readonly (Planets planet, string symbol, string name)[] PlanetOrder =
    [
        (Planets.Sun, "⊙", "Sun"),
        (Planets.Earth, "⊕", "Earth"),
        (Planets.NorthNode, "Ω", "N.Node"),
        (Planets.SouthNode, "☋", "S.Node"),
        (Planets.Moon, "◑", "Moon"),
        (Planets.Mercury, "☿", "Mercury"),
        (Planets.Venus, "♀", "Venus"),
        (Planets.Mars, "♂", "Mars"),
        (Planets.Jupiter, "♃", "Jupiter"),
        (Planets.Saturn, "♄", "Saturn"),
        (Planets.Uranus, "♅", "Uranus"),
        (Planets.Neptune, "♆", "Neptune"),
        (Planets.Pluto, "♇", "Pluto"),
    ];

    public byte[] GenerateBodygraphPng(HumanDesignChart chart, HumanDesignTransitChart? transit = null)
    {
        var svg = GenerateBodygraphSvg(chart, transit);
        return SvgToPng(svg);
    }

    private static string FormatDefinition(SplitDefinitions def) => def switch
    {
        SplitDefinitions.SingleDefinition => "Single Definition",
        SplitDefinitions.SplitDefinition => "Split Definition",
        SplitDefinitions.TripleSplit => "Triple Split",
        SplitDefinitions.QuadrupleSplit => "Quadruple Split",
        _ => "No Definition"
    };

    private static string FormatIncarnationCross(string raw)
    {
        var sb2 = new StringBuilder();
        for (int i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            if (sb2.Length > 0)
            {
                var prev = sb2[sb2.Length - 1];
                // Space before uppercase after lowercase, or before digit after letter, or before letter after digit
                if ((char.IsUpper(c) && char.IsLower(prev)) ||
                    (char.IsDigit(c) && char.IsLetter(prev)) ||
                    (char.IsLetter(c) && char.IsDigit(prev)))
                    sb2.Append(' ');
            }
            sb2.Append(c);
        }
        return sb2.ToString();
    }

    private static (string notSelf, string signature) GetNotSelfAndSignature(Types type) => type switch
    {
        Types.Generator => ("Frustration", "Satisfaction"),
        Types.ManifestingGenerator => ("Frustration", "Satisfaction"),
        Types.Manifestor => ("Anger", "Peace"),
        Types.Projector => ("Bitterness", "Success"),
        Types.Reflector => ("Disappointment", "Surprise"),
        _ => ("", "")
    };

    private string GenerateBodygraphSvg(HumanDesignChart chart, HumanDesignTransitChart? transit = null)
    {
        var sb = new StringBuilder();

        const int W = 960, H = 1200;
        // Bodygraph is drawn in a 500x750 coordinate space, offset to center
        const float BgOffX = 230f, BgOffY = 115f;
        const float S = 500f;

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {W} {H}\" width=\"{W}\" height=\"{H}\">");

        // Background
        sb.AppendLine($"<rect width=\"{W}\" height=\"{H}\" fill=\"{BgColor}\"/>");
        sb.AppendLine($"<rect x=\"8\" y=\"8\" width=\"{W - 16}\" height=\"{H - 16}\" fill=\"none\" stroke=\"#d5d0c8\" stroke-width=\"1\" rx=\"6\"/>");

        // Defs: gradients and patterns
        sb.AppendLine("<defs>");
        // Center gradients
        foreach (var center in Enum.GetValues<Centers>())
        {
            var dark = CenterColorMap[center];
            var light = CenterGradientLight[center];
            sb.AppendLine($"<linearGradient id=\"grad-{center}\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\">");
            sb.AppendLine($"  <stop offset=\"0%\" stop-color=\"{light}\"/>");
            sb.AppendLine($"  <stop offset=\"100%\" stop-color=\"{dark}\"/>");
            sb.AppendLine("</linearGradient>");
        }
        // Stripe pattern for mixed channels
        sb.AppendLine("<pattern id=\"mixed-stripe\" patternUnits=\"userSpaceOnUse\" width=\"8\" height=\"8\" patternTransform=\"rotate(45)\">");
        sb.AppendLine($"  <rect width=\"4\" height=\"8\" fill=\"{PersonalityColor}\"/>");
        sb.AppendLine($"  <rect x=\"4\" width=\"4\" height=\"8\" fill=\"{DesignColor}\"/>");
        sb.AppendLine("</pattern>");
        sb.AppendLine("</defs>");

        // Style
        sb.AppendLine("<style>");
        sb.AppendLine($".center {{ stroke: #555; stroke-width: 1.5; }}");
        sb.AppendLine($".center-undef {{ stroke: #999; stroke-width: 1.5; fill: white; }}");
        sb.AppendLine($".gate-text {{ font-family: {FontFamily}; font-size: 11px; font-weight: bold; text-anchor: middle; dominant-baseline: central; pointer-events: none; }}");
        sb.AppendLine($".header-text {{ font-family: {FontFamily}; font-size: 15px; text-anchor: middle; fill: #333; }}");
        sb.AppendLine($".header-sub {{ font-family: {FontFamily}; font-size: 12px; text-anchor: middle; fill: #666; }}");
        sb.AppendLine($".planet-text {{ font-family: {FontFamily}; font-size: 12px; fill: #333; }}");
        sb.AppendLine($".planet-header {{ font-family: {FontFamily}; font-size: 13px; font-weight: bold; }}");
        sb.AppendLine($".info-text {{ font-family: {FontFamily}; font-size: 11px; text-anchor: middle; fill: #888; }}");
        sb.AppendLine("</style>");

        // === HEADER ===
        var typeStr = chart.Type switch
        {
            Types.ManifestingGenerator => "Manifesting Generator",
            Types.Generator => "Generator",
            Types.Manifestor => "Manifestor",
            Types.Projector => "Projector",
            Types.Reflector => "Reflector",
            _ => chart.Type.ToString()
        };
        var profileStr = FormatProfile(chart.Profile);
        var authorityStr = chart.Strategy.ToString();
        var definitionStr = FormatDefinition(chart.SplitDefinition);
        var crossStr = FormatIncarnationCross(chart.IncarnationCross.ToString());

        sb.AppendLine($"<text x=\"{W / 2}\" y=\"40\" class=\"header-text\" font-weight=\"bold\">{typeStr}  ·  {profileStr}  ·  {authorityStr}</text>");
        sb.AppendLine($"<text x=\"{W / 2}\" y=\"60\" class=\"header-sub\">{definitionStr}</text>");
        sb.AppendLine($"<text x=\"{W / 2}\" y=\"78\" style=\"font-family: {FontFamily}; font-size: 11px; text-anchor: middle; fill: #888;\">{crossStr}</text>");

        // === PLANET TABLES ===
        DrawPlanetTable(sb, chart.PersonalityActivation, "Personality", 20, 120, PersonalityColor, false);
        DrawPlanetTable(sb, chart.DesignActivation, "Design", W - 155, 120, DesignColor, true);

        // === BODYGRAPH (translated) ===
        sb.AppendLine($"<g transform=\"translate({BgOffX},{BgOffY})\">");

        var gateAct = chart.GateActivations;

        // Transit handling
        HashSet<Channels>? transitOnlyChannels = null;
        if (transit != null)
        {
            transitOnlyChannels = new HashSet<Channels>(
                transit.ChannelActivations.Keys.Except(chart.ActiveChannels));
        }

        // === CURVED CHANNELS ===
        foreach (var (channel, x1, y1, tx1, ty1, mx, my, x2, y2, tx2, ty2) in CurvedChannels)
        {
            var (gate1, gate2) = channel.ToGates();
            DrawCurvedChannelHalf(sb, gateAct.GetValueOrDefault(gate1),
                x1 * S, y1 * S, tx1 * S, ty1 * S, mx * S, my * S);
            DrawCurvedChannelHalf(sb, gateAct.GetValueOrDefault(gate2),
                mx * S, my * S, mx * S, my * S, tx2 * S, ty2 * S, x2 * S, y2 * S);

            if (transitOnlyChannels?.Contains(channel) == true)
            {
                sb.AppendLine($"<path d=\"M {F(x1 * S)} {F(y1 * S)} C {F(tx1 * S)} {F(ty1 * S)} {F(mx * S)} {F(my * S)} {F(mx * S)} {F(my * S)} C {F(mx * S)} {F(my * S)} {F(tx2 * S)} {F(ty2 * S)} {F(x2 * S)} {F(y2 * S)}\" fill=\"none\" stroke=\"{TransitColor}\" stroke-width=\"5\" stroke-dasharray=\"6,4\" opacity=\"0.6\"/>");
            }
        }

        // === LINE CHANNELS ===
        foreach (var (channel, x1, y1, x2, y2) in LineChannels)
        {
            var (gate1, gate2) = channel.ToGates();
            float mx2 = (x1 + x2) / 2 * S, my2 = (y1 + y2) / 2 * S;

            DrawLineHalf(sb, gateAct.GetValueOrDefault(gate1), x1 * S, y1 * S, mx2, my2);
            DrawLineHalf(sb, gateAct.GetValueOrDefault(gate2), mx2, my2, x2 * S, y2 * S);

            if (transitOnlyChannels?.Contains(channel) == true)
            {
                sb.AppendLine($"<line x1=\"{F(x1 * S)}\" y1=\"{F(y1 * S)}\" x2=\"{F(x2 * S)}\" y2=\"{F(y2 * S)}\" stroke=\"{TransitColor}\" stroke-width=\"5\" stroke-dasharray=\"6,4\" opacity=\"0.6\"/>");
            }
        }

        // === CENTERS ===
        DrawCrown(sb, chart, S);
        DrawMind(sb, chart, S);
        DrawThroat(sb, chart, S);
        DrawSelf(sb, chart, S);
        DrawHeart(sb, chart, S);
        DrawEmotions(sb, chart, S);
        DrawSpleen(sb, chart, S);
        DrawSacral(sb, chart, S);
        DrawRoot(sb, chart, S);

        sb.AppendLine("</g>"); // end bodygraph group

        // Footer
        var strategyStr = chart.Type switch
        {
            Types.Manifestor => "To Inform",
            Types.Generator => "Wait To Respond",
            Types.ManifestingGenerator => "Wait To Respond",
            Types.Projector => "Wait For The Invitation",
            Types.Reflector => "Wait A Lunar Cycle",
            _ => chart.Type.ToString()
        };
        var (notSelf, signature) = GetNotSelfAndSignature(chart.Type);
        sb.AppendLine($"<text x=\"{W / 2}\" y=\"{H - 35}\" class=\"info-text\">Strategy: {strategyStr}  ·  Signature: {signature}  ·  Not-Self: {notSelf}</text>");

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static void DrawPlanetTable(StringBuilder sb, Dictionary<Planets, Activation> activations,
        string title, float x, float y, string color, bool alignRight)
    {
        float colWidth = 135f;
        float rowH = 22f;

        // Background card
        float cardH = 20 + PlanetOrder.Length * rowH + 10;
        sb.AppendLine($"<rect x=\"{F(x - 5)}\" y=\"{F(y - 15)}\" width=\"{F(colWidth + 10)}\" height=\"{F(cardH)}\" rx=\"6\" fill=\"white\" stroke=\"{color}\" stroke-width=\"1\" opacity=\"0.6\"/>");

        // Title
        float titleX = x + colWidth / 2;
        sb.AppendLine($"<text x=\"{F(titleX)}\" y=\"{F(y + 2)}\" class=\"planet-header\" fill=\"{color}\" text-anchor=\"middle\">{title}</text>");

        float startY = y + 22;
        foreach (var (planet, symbol, name) in PlanetOrder)
        {
            if (!activations.TryGetValue(planet, out var act)) continue;

            var gateNum = act.Gate.ToNumber();
            var line = (int)act.Line;

            sb.AppendLine($"<text x=\"{F(x + 5)}\" y=\"{F(startY)}\" class=\"planet-text\">{symbol} {name}</text>");
            sb.AppendLine($"<text x=\"{F(x + colWidth - 5)}\" y=\"{F(startY)}\" class=\"planet-text\" text-anchor=\"end\" font-weight=\"bold\">{gateNum}.{line}</text>");

            startY += rowH;
        }
    }

    private static string F(float v) => v.ToString("F1", CultureInfo.InvariantCulture);

    private static string CenterFill(HumanDesignChart chart, Centers center) =>
        chart.ConnectedComponents.ContainsKey(center) ? $"url(#grad-{center})" : "white";

    private static string CenterClass(HumanDesignChart chart, Centers center) =>
        chart.ConnectedComponents.ContainsKey(center) ? "center" : "center-undef";

    private static void DrawLineHalf(StringBuilder sb, ActivationTypes act, float x1, float y1, float x2, float y2)
    {
        string stroke = act switch
        {
            ActivationTypes.FirstComparator => PersonalityColor,
            ActivationTypes.SecondComparator => DesignColor,
            ActivationTypes.Mixed => "url(#mixed-stripe)",
            _ => InactiveColor
        };
        float sw = act == ActivationTypes.None ? 3f : 5f;
        sb.AppendLine($"<line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" stroke=\"{stroke}\" stroke-width=\"{F(sw)}\" stroke-linecap=\"round\"/>");
    }

    private static void DrawCurvedChannelHalf(StringBuilder sb, ActivationTypes act,
        float x1, float y1, float tx1, float ty1, float mx, float my)
    {
        var path = $"M {F(x1)} {F(y1)} C {F(tx1)} {F(ty1)} {F(mx)} {F(my)} {F(mx)} {F(my)}";
        string stroke = act switch
        {
            ActivationTypes.FirstComparator => PersonalityColor,
            ActivationTypes.SecondComparator => DesignColor,
            ActivationTypes.Mixed => "url(#mixed-stripe)",
            _ => InactiveColor
        };
        float sw = act == ActivationTypes.None ? 3f : 5f;
        sb.AppendLine($"<path d=\"{path}\" fill=\"none\" stroke=\"{stroke}\" stroke-width=\"{F(sw)}\" stroke-linecap=\"round\"/>");
    }

    private static void DrawCurvedChannelHalf(StringBuilder sb, ActivationTypes act,
        float mx, float my, float mx2, float my2, float tx2, float ty2, float x2, float y2)
    {
        var path = $"M {F(mx)} {F(my)} C {F(mx2)} {F(my2)} {F(tx2)} {F(ty2)} {F(x2)} {F(y2)}";
        string stroke = act switch
        {
            ActivationTypes.FirstComparator => PersonalityColor,
            ActivationTypes.SecondComparator => DesignColor,
            ActivationTypes.Mixed => "url(#mixed-stripe)",
            _ => InactiveColor
        };
        float sw = act == ActivationTypes.None ? 3f : 5f;
        sb.AppendLine($"<path d=\"{path}\" fill=\"none\" stroke=\"{stroke}\" stroke-width=\"{F(sw)}\" stroke-linecap=\"round\"/>");
    }

    private static void DrawGate(StringBuilder sb, HumanDesignChart chart, Gates gate, float cx, float cy)
    {
        const float r = 12f;
        var act = chart.GateActivations.GetValueOrDefault(gate);
        var num = gate.ToNumber();

        switch (act)
        {
            case ActivationTypes.FirstComparator:
                sb.AppendLine($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"{PersonalityColor}\" stroke=\"#444\" stroke-width=\"0.8\"/>");
                sb.AppendLine($"<text x=\"{F(cx)}\" y=\"{F(cy)}\" fill=\"white\" class=\"gate-text\">{num}</text>");
                break;
            case ActivationTypes.SecondComparator:
                sb.AppendLine($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"{DesignColor}\" stroke=\"#994466\" stroke-width=\"0.8\"/>");
                sb.AppendLine($"<text x=\"{F(cx)}\" y=\"{F(cy)}\" fill=\"white\" class=\"gate-text\">{num}</text>");
                break;
            case ActivationTypes.Mixed:
                sb.AppendLine($"<clipPath id=\"gl{num}\"><rect x=\"{F(cx - r)}\" y=\"{F(cy - r)}\" width=\"{F(r)}\" height=\"{F(r * 2)}\"/></clipPath>");
                sb.AppendLine($"<clipPath id=\"gr{num}\"><rect x=\"{F(cx)}\" y=\"{F(cy - r)}\" width=\"{F(r)}\" height=\"{F(r * 2)}\"/></clipPath>");
                sb.AppendLine($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"{DesignColor}\" clip-path=\"url(#gl{num})\" stroke=\"none\"/>");
                sb.AppendLine($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"{PersonalityColor}\" clip-path=\"url(#gr{num})\" stroke=\"none\"/>");
                sb.AppendLine($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"none\" stroke=\"#666\" stroke-width=\"0.8\"/>");
                sb.AppendLine($"<text x=\"{F(cx)}\" y=\"{F(cy)}\" fill=\"white\" class=\"gate-text\">{num}</text>");
                break;
            default:
                sb.AppendLine($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"white\" stroke=\"#999\" stroke-width=\"1.2\"/>");
                sb.AppendLine($"<text x=\"{F(cx)}\" y=\"{F(cy)}\" fill=\"#555\" class=\"gate-text\">{num}</text>");
                break;
        }
    }

    // === CENTER DRAWING ===

    private static void DrawCrown(StringBuilder sb, HumanDesignChart chart, float s)
    {
        float ox = 0.4f * s, oy = 0f, w = 0.2f * s, h = 0.15f * s;
        float scaleX = w, scaleY = h / 0.75f;

        var p1 = $"{F(ox)},{F(oy + 0.75f * scaleY)}";
        var p2 = $"{F(ox + scaleX)},{F(oy + 0.75f * scaleY)}";
        var p3 = $"{F(ox + 0.5f * scaleX)},{F(oy)}";
        sb.AppendLine($"<polygon points=\"{p1} {p2} {p3}\" class=\"{CenterClass(chart, Centers.Crown)}\" fill=\"{CenterFill(chart, Centers.Crown)}\"/>");

        foreach (var (gate, lx, ly) in new[] {
            (Gates.Key64, 0.25f, 0.63f), (Gates.Key61, 0.50f, 0.63f), (Gates.Key63, 0.75f, 0.63f)
        }) DrawGate(sb, chart, gate, ox + lx * scaleX, oy + ly * scaleY);
    }

    private static void DrawMind(StringBuilder sb, HumanDesignChart chart, float s)
    {
        float ox = 0.4f * s, oy = 0.2f * s, w = 0.2f * s, h = 0.15f * s;
        float scaleX = w, scaleY = h / 0.75f;

        var p1 = $"{F(ox)},{F(oy)}";
        var p2 = $"{F(ox + scaleX)},{F(oy)}";
        var p3 = $"{F(ox + 0.5f * scaleX)},{F(oy + 0.75f * scaleY)}";
        sb.AppendLine($"<polygon points=\"{p1} {p2} {p3}\" class=\"{CenterClass(chart, Centers.Mind)}\" fill=\"{CenterFill(chart, Centers.Mind)}\"/>");

        foreach (var (gate, lx, ly) in new[] {
            (Gates.Key47, 0.25f, 0.08f), (Gates.Key24, 0.50f, 0.08f), (Gates.Key4, 0.75f, 0.08f),
            (Gates.Key17, 0.35f, 0.36f), (Gates.Key43, 0.50f, 0.58f), (Gates.Key11, 0.65f, 0.36f)
        }) DrawGate(sb, chart, gate, ox + lx * scaleX, oy + ly * scaleY);
    }

    private static void DrawThroat(StringBuilder sb, HumanDesignChart chart, float s)
    {
        float ox = 0.4f * s, oy = 0.425f * s, w = 0.2f * s, h = 0.2f * s;
        float r = 0.1f * w;
        sb.AppendLine($"<rect x=\"{F(ox)}\" y=\"{F(oy)}\" width=\"{F(w)}\" height=\"{F(h)}\" rx=\"{F(r)}\" ry=\"{F(r)}\" class=\"{CenterClass(chart, Centers.Throat)}\" fill=\"{CenterFill(chart, Centers.Throat)}\"/>");

        foreach (var (gate, lx, ly) in new[] {
            (Gates.Key62, 0.25f, 0.10f), (Gates.Key23, 0.50f, 0.10f), (Gates.Key56, 0.75f, 0.10f),
            (Gates.Key16, 0.10f, 0.30f), (Gates.Key35, 0.90f, 0.30f),
            (Gates.Key12, 0.90f, 0.50f),
            (Gates.Key20, 0.10f, 0.70f), (Gates.Key45, 0.90f, 0.70f),
            (Gates.Key31, 0.25f, 0.90f), (Gates.Key8, 0.50f, 0.90f), (Gates.Key33, 0.75f, 0.90f)
        }) DrawGate(sb, chart, gate, ox + lx * w, oy + ly * h);
    }

    private static void DrawSelf(StringBuilder sb, HumanDesignChart chart, float s)
    {
        float ox = 0.4f * s, oy = 0.71f * s, w = 0.2f * s, h = 0.2f * s;
        var pts = $"{F(ox + 0.5f * w)},{F(oy)} {F(ox + w)},{F(oy + 0.5f * h)} {F(ox + 0.5f * w)},{F(oy + h)} {F(ox)},{F(oy + 0.5f * h)}";
        sb.AppendLine($"<polygon points=\"{pts}\" class=\"{CenterClass(chart, Centers.Self)}\" fill=\"{CenterFill(chart, Centers.Self)}\"/>");

        foreach (var (gate, lx, ly) in new[] {
            (Gates.Key1, 0.50f, 0.15f),
            (Gates.Key7, 0.32f, 0.32f), (Gates.Key13, 0.67f, 0.32f),
            (Gates.Key10, 0.15f, 0.50f), (Gates.Key25, 0.85f, 0.50f),
            (Gates.Key15, 0.32f, 0.68f), (Gates.Key46, 0.67f, 0.68f),
            (Gates.Key2, 0.50f, 0.85f)
        }) DrawGate(sb, chart, gate, ox + lx * w, oy + ly * h);
    }

    private static void DrawHeart(StringBuilder sb, HumanDesignChart chart, float s)
    {
        float ox = 0.62f * s, oy = 0.82f * s, w = 0.15f * s, h = 0.15f * s;
        var pts = $"{F(ox)},{F(oy + 0.8f * h)} {F(ox + 0.8f * w)},{F(oy)} {F(ox + w)},{F(oy + h)}";
        sb.AppendLine($"<polygon points=\"{pts}\" class=\"{CenterClass(chart, Centers.Heart)}\" fill=\"{CenterFill(chart, Centers.Heart)}\"/>");

        foreach (var (gate, lx, ly) in new[] {
            (Gates.Key26, 0.28f, 0.72f), (Gates.Key51, 0.50f, 0.50f),
            (Gates.Key21, 0.71f, 0.28f), (Gates.Key40, 0.83f, 0.83f)
        }) DrawGate(sb, chart, gate, ox + lx * w, oy + ly * h);
    }

    private static void DrawEmotions(StringBuilder sb, HumanDesignChart chart, float s)
    {
        float ox = 0.75f * s, oy = 0.9f * s, w = 0.25f * s, h = 0.25f * s;
        var pts = $"{F(ox + w)},{F(oy)} {F(ox + w)},{F(oy + h)} {F(ox + 0.2f * w)},{F(oy + 0.5f * h)}";
        sb.AppendLine($"<polygon points=\"{pts}\" class=\"{CenterClass(chart, Centers.Emotions)}\" fill=\"{CenterFill(chart, Centers.Emotions)}\"/>");

        foreach (var (gate, lx, ly) in new[] {
            (Gates.Key36, 0.88f, 0.18f), (Gates.Key22, 0.71f, 0.29f),
            (Gates.Key37, 0.53f, 0.40f), (Gates.Key6, 0.37f, 0.50f),
            (Gates.Key49, 0.53f, 0.60f), (Gates.Key55, 0.71f, 0.71f),
            (Gates.Key30, 0.88f, 0.82f)
        }) DrawGate(sb, chart, gate, ox + lx * w, oy + ly * h);
    }

    private static void DrawSpleen(StringBuilder sb, HumanDesignChart chart, float s)
    {
        float ox = 0f, oy = 0.9f * s, w = 0.25f * s, h = 0.25f * s;
        var pts = $"{F(ox)},{F(oy)} {F(ox)},{F(oy + h)} {F(ox + 0.8f * w)},{F(oy + 0.5f * h)}";
        sb.AppendLine($"<polygon points=\"{pts}\" class=\"{CenterClass(chart, Centers.Spleen)}\" fill=\"{CenterFill(chart, Centers.Spleen)}\"/>");

        foreach (var (gate, lx, ly) in new[] {
            (Gates.Key48, 0.13f, 0.18f), (Gates.Key57, 0.28f, 0.28f),
            (Gates.Key44, 0.45f, 0.38f), (Gates.Key50, 0.61f, 0.49f),
            (Gates.Key32, 0.45f, 0.63f), (Gates.Key28, 0.28f, 0.73f),
            (Gates.Key18, 0.13f, 0.83f)
        }) DrawGate(sb, chart, gate, ox + lx * w, oy + ly * h);
    }

    private static void DrawSacral(StringBuilder sb, HumanDesignChart chart, float s)
    {
        float ox = 0.4f * s, oy = 1.0f * s, w = 0.2f * s, h = 0.2f * s;
        float r = 0.1f * w;
        sb.AppendLine($"<rect x=\"{F(ox)}\" y=\"{F(oy)}\" width=\"{F(w)}\" height=\"{F(h)}\" rx=\"{F(r)}\" ry=\"{F(r)}\" class=\"{CenterClass(chart, Centers.Sacral)}\" fill=\"{CenterFill(chart, Centers.Sacral)}\"/>");

        foreach (var (gate, lx, ly) in new[] {
            (Gates.Key5, 0.25f, 0.10f), (Gates.Key14, 0.50f, 0.10f), (Gates.Key29, 0.75f, 0.10f),
            (Gates.Key34, 0.10f, 0.30f), (Gates.Key59, 0.90f, 0.70f),
            (Gates.Key27, 0.10f, 0.70f),
            (Gates.Key42, 0.25f, 0.90f), (Gates.Key3, 0.50f, 0.90f), (Gates.Key9, 0.75f, 0.90f)
        }) DrawGate(sb, chart, gate, ox + lx * w, oy + ly * h);
    }

    private static void DrawRoot(StringBuilder sb, HumanDesignChart chart, float s)
    {
        float ox = 0.4f * s, oy = 1.31f * s, w = 0.2f * s, h = 0.2f * s;
        float r = 0.1f * w;
        sb.AppendLine($"<rect x=\"{F(ox)}\" y=\"{F(oy)}\" width=\"{F(w)}\" height=\"{F(h)}\" rx=\"{F(r)}\" ry=\"{F(r)}\" class=\"{CenterClass(chart, Centers.Root)}\" fill=\"{CenterFill(chart, Centers.Root)}\"/>");

        foreach (var (gate, lx, ly) in new[] {
            (Gates.Key53, 0.25f, 0.10f), (Gates.Key60, 0.50f, 0.10f), (Gates.Key52, 0.75f, 0.10f),
            (Gates.Key54, 0.10f, 0.30f), (Gates.Key19, 0.90f, 0.30f),
            (Gates.Key38, 0.10f, 0.55f), (Gates.Key39, 0.90f, 0.55f),
            (Gates.Key58, 0.10f, 0.80f), (Gates.Key41, 0.90f, 0.80f)
        }) DrawGate(sb, chart, gate, ox + lx * w, oy + ly * h);
    }

    // === CHANNEL DATA ===

    private static readonly (Channels channel, float x1, float y1, float tx1, float ty1, float mx, float my, float x2, float y2, float tx2, float ty2)[] CurvedChannels =
    [
        (Channels.Key34Key57, 0.42f, 1.07f, 0.4f, 1f, 0.32f, 0.93f, 0.075f, 0.97f, 0.2f, 0.8f),
        (Channels.Key10Key20, 0.42f, 0.8f, 0.36f, 0.75f, 0.37f, 0.7f, 0.41f, 0.57f, 0.4f, 0.55f),
        (Channels.Key10Key57, 0.42f, 0.8f, 0.34f, 0.8f, 0.245f, 0.845f, 0.08f, 0.96f, 0.13f, 0.9f),
        (Channels.Key10Key34, 0.42f, 0.8f, 0.32f, 0.9f, 0.322f, 0.94f, 0.42f, 1.08f, 0.32f, 1.08f),
        (Channels.Key20Key34, 0.42f, 0.56f, 0.32f, 0.66f, 0.32f, 0.8f, 0.42f, 1.07f, 0.32f, 1f),
    ];

    private static readonly (Channels channel, float x1, float y1, float x2, float y2)[] LineChannels =
    [
        (Channels.Key26Key44, 0.66f, 0.925f, 0.12f, 0.99f),
        (Channels.Key42Key53, 0.45f, 1.2f, 0.45f, 1.3f),
        (Channels.Key3Key60, 0.50f, 1.2f, 0.50f, 1.3f),
        (Channels.Key9Key52, 0.55f, 1.2f, 0.55f, 1.3f),
        (Channels.Key30Key41, 0.98f, 1.1f, 0.58f, 1.47f),
        (Channels.Key39Key55, 0.58f, 1.42f, 0.96f, 1.05f),
        (Channels.Key19Key49, 0.58f, 1.37f, 0.91f, 1.03f),
        (Channels.Key18Key58, 0.025f, 1.1f, 0.41f, 1.46f),
        (Channels.Key28Key38, 0.07f, 1.085f, 0.41f, 1.41f),
        (Channels.Key32Key54, 0.11f, 1.05f, 0.41f, 1.36f),
        (Channels.Key6Key59, 0.84f, 1.02f, 0.58f, 1.145f),
        (Channels.Key27Key50, 0.41f, 1.14f, 0.16f, 1.02f),
        (Channels.Key5Key15, 0.45f, 1f, 0.46f, 0.85f),
        (Channels.Key2Key14, 0.5f, 0.85f, 0.5f, 1f),
        (Channels.Key29Key46, 0.55f, 1f, 0.54f, 0.85f),
        (Channels.Key37Key40, 0.88f, 0.995f, 0.74f, 0.94f),
        (Channels.Key12Key22, 0.59f, 0.52f, 0.925f, 0.96f),
        (Channels.Key35Key36, 0.59f, 0.48f, 0.965f, 0.94f),
        (Channels.Key21Key45, 0.73f, 0.86f, 0.58f, 0.55f),
        (Channels.Key25Key51, 0.58f, 0.8f, 0.69f, 0.89f),
        (Channels.Key16Key48, 0.42f, 0.48f, 0.03f, 0.94f),
        (Channels.Key20Key57, 0.42f, 0.56f, 0.07f, 0.97f),
        (Channels.Key7Key31, 0.46f, 0.76f, 0.45f, 0.6f),
        (Channels.Key1Key8, 0.5f, 0.73f, 0.5f, 0.6f),
        (Channels.Key13Key33, 0.54f, 0.75f, 0.55f, 0.6f),
        (Channels.Key17Key62, 0.465f, 0.27f, 0.45f, 0.45f),
        (Channels.Key23Key43, 0.5f, 0.45f, 0.5f, 0.32f),
        (Channels.Key11Key56, 0.535f, 0.27f, 0.55f, 0.45f),
        (Channels.Key47Key64, 0.45f, 0.22f, 0.45f, 0.12f),
        (Channels.Key24Key61, 0.5f, 0.22f, 0.5f, 0.12f),
        (Channels.Key4Key63, 0.55f, 0.22f, 0.55f, 0.12f),
    ];

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

    private byte[] SvgToPng(string svgContent)
    {
        var svg = new SKSvg();
        svg.FromSvg(svgContent);
        if (svg.Picture == null)
            throw new Exception("Failed to parse SVG");

        var bounds = svg.Picture.CullRect;
        var width = (int)bounds.Width;
        var height = (int)bounds.Height;

        using var surface = SKSurface.Create(new SKImageInfo(width * 2, height * 2));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        canvas.Scale(2);
        canvas.DrawPicture(svg.Picture);
        canvas.Flush();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }
}
