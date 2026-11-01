using ModelContextProtocol;

namespace PlotlyTools.Charts;

/// <summary>KPI indicators: one number, optionally compared with a reference, as a big number or a gauge.</summary>
internal static partial class IndicatorBuilder
{
    public static BuiltChart Build(IndicatorSpec spec)
    {
        if (spec.Value is not { } value || !double.IsFinite(value))
        {
            throw new McpException("Pass the number to show in value, e.g. 1250000.");
        }

        var reference = spec.Reference;
        var mode = ChartBuilder.Choose(spec.Mode, "mode", reference is null ? "number" : "delta", ("number", "number"), ("delta", "delta"), ("gauge", "gauge"));
        if (mode == "delta" && reference is null)
        {
            throw new McpException("mode 'delta' shows the change from reference, so pass reference too.");
        }

        var trace = ChartBuilder.NewTrace("indicator", null);
        trace.SetValue("value", value);
        trace.SetValue("mode", mode switch
        {
            "number" => "number",
            "delta" => "number+delta",
            _ => reference is null ? "gauge+number" : "gauge+number+delta",
        });

        var number = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(spec.Prefix))
        {
            number["prefix"] = spec.Prefix;
        }

        if (!string.IsNullOrEmpty(spec.Suffix))
        {
            number["suffix"] = spec.Suffix;
        }

        trace.SetValue("number", number);
        if (reference is not null)
        {
            trace.SetValue("delta", new Dictionary<string, object> { ["reference"] = reference.Value });
        }

        // The title sits on the indicator itself, centred over the number, rather than in the chart's corner.
        if (!string.IsNullOrWhiteSpace(spec.Title))
        {
            trace.SetValue("title", new Dictionary<string, object> { ["text"] = spec.Title });
        }

        if (mode == "gauge")
        {
            trace.SetValue("gauge", Gauge(value, reference, spec.Min, spec.Max));
        }

        return new BuiltChart(ChartBuilder.Combine([trace], ChartBuilder.NewLayout(null)), 1, 1, []);
    }

    private static Dictionary<string, object> Gauge(double value, double? reference, double? min, double? max)
    {
        var low = min ?? 0;
        var high = max ?? NiceMax(Math.Max(value, reference ?? value));
        if (high <= low)
        {
            throw new McpException($"The gauge needs max greater than min (min {low}, max {high}).");
        }

        var gauge = new Dictionary<string, object>
        {
            ["axis"] = new Dictionary<string, object> { ["range"] = new[] { low, high } },
            ["bar"] = new Dictionary<string, object> { ["color"] = ChartStyle.LightColorway[0] },
        };

        if (reference is not null)
        {
            gauge["threshold"] = new Dictionary<string, object>
            {
                ["value"] = reference.Value,
                ["thickness"] = 0.75,
                ["line"] = new Dictionary<string, object> { ["color"] = "#e11d48", ["width"] = 3 },
            };
        }

        return gauge;
    }

    /// <summary>A round number a little above <paramref name="value"/>, so the gauge isn't full: 1,250,000 → 1,500,000.</summary>
    public static double NiceMax(double value)
    {
        if (value <= 0)
        {
            return 1;
        }

        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
        return Math.Ceiling(value * 1.1 / magnitude * 2) / 2 * magnitude;
    }
}
