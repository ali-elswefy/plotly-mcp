namespace PlotlyTools.Charts;

// What each chart type is asked to draw, with the same names as the tool parameters. Chart tools fill them from
// their parameters; dashboards will read them from each item's JSON. Null means "use the default", which the
// builders decide, so both callers get the same defaults. The data travels separately, as a ChartTable.

internal sealed partial record BarChartSpec(
    string? X,
    IReadOnlyList<string>? Y,
    string? Series,
    string? Orientation,
    string? BarMode,
    string? Aggregate,
    string? Sort,
    bool? ShowValues,
    string? Title,
    string? XTitle,
    string? YTitle);

internal sealed partial record LineChartSpec(
    string? X,
    IReadOnlyList<string>? Y,
    string? Series,
    bool? Markers,
    string? Shape,
    string? Title,
    string? XTitle,
    string? YTitle);

internal sealed partial record AreaChartSpec(
    string? X,
    IReadOnlyList<string>? Y,
    string? Series,
    bool? Stacked,
    bool? Normalize,
    string? Title,
    string? XTitle,
    string? YTitle);

internal sealed partial record ScatterChartSpec(
    string? X,
    string? Y,
    string? Series,
    string? Size,
    string? Label,
    bool? LogX,
    bool? LogY,
    string? Title,
    string? XTitle,
    string? YTitle);

internal sealed partial record HistogramSpec(
    string? X,
    string? Series,
    int? Bins,
    string? Normalize,
    bool? Cumulative,
    string? Title,
    string? XTitle,
    string? YTitle);

internal sealed partial record PieChartSpec(
    string? Labels,
    string? Values,
    string? Aggregate,
    double? Hole,
    int? MaxSlices,
    bool? ShowPercent,
    string? Title);

internal sealed partial record BoxPlotSpec(
    string? Y,
    string? X,
    string? Series,
    string? Style,
    string? Points,
    string? Title,
    string? XTitle,
    string? YTitle);

internal sealed partial record HeatmapSpec(
    string? X,
    string? Y,
    string? Z,
    string? Aggregate,
    string? ColorScale,
    bool? ShowValues,
    string? Title,
    string? XTitle,
    string? YTitle);

internal sealed partial record FunnelChartSpec(
    string? Stage,
    string? Values,
    string? Series,
    string? Aggregate,
    string? Title);

/// <summary>Treemaps and sunbursts draw the same hierarchy, so they share one spec.</summary>
internal sealed partial record HierarchySpec(
    IReadOnlyList<string>? Path,
    string? Values,
    string? Aggregate,
    int? MaxDepth,
    string? Title);

internal sealed partial record WaterfallChartSpec(
    string? Labels,
    string? Values,
    string? Measure,
    bool? ShowTotal,
    string? Title,
    string? XTitle,
    string? YTitle);

/// <summary>An indicator shows one number passed directly, so it has no table.</summary>
internal sealed partial record IndicatorSpec(
    double? Value,
    double? Reference,
    string? Mode,
    double? Min,
    double? Max,
    string? Prefix,
    string? Suffix,
    string? Title);

/// <summary>A builder's output: the chart, before styling and rendering, and what to tell the agent about it.</summary>
internal sealed partial record BuiltChart(Plotly.NET.GenericChart Chart, int Traces, int Points, IReadOnlyList<string> Notes);
