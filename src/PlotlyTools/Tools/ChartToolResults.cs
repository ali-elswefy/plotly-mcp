using System.Text.Json.Serialization;

namespace PlotlyTools.Tools;

// Wire shapes returned to agents by the chart and dashboard tools. Optional fields are omitted when empty to keep
// responses small.

/// <summary>The summary block that comes before the dashboard itself.</summary>
public sealed partial record DashboardSummary(
    string Title,
    string Format,
    int Items,
    int Charts,
    int Points,
    bool Truncated,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? FilePath,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Note);

/// <summary>The summary block that comes before the chart itself.</summary>
public sealed partial record ChartSummary(
    string ChartType,
    string Format,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Title,
    int Traces,
    int Points,
    bool Truncated,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? FilePath,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Note);
