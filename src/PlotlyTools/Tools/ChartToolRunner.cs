using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using PlotlyTools.Charts;
using PlotlyTools.Configuration;
using PlotlyTools.Rendering;

namespace PlotlyTools.Tools;

/// <summary>
/// The steps every chart tool takes around its builder: check the output parameters, parse the data, build,
/// render, and wrap the result. Output parameters are checked first so a bad format fails before any work.
/// </summary>
internal sealed partial class ChartToolRunner(ChartRenderer renderer, IOptions<PlotlyToolsOptions> options)
{
    private readonly ChartRenderer _renderer = renderer;
    private readonly PlotlyToolsOptions _options = options.Value;

    public int MaxSeries => _options.MaxSeries;

    public async Task<CallToolResult> RunAsync(
        string chartType,
        ChartDataInput? data,
        string? title,
        string? format,
        string? theme,
        int? width,
        int? height,
        Func<ChartTable, BuiltChart> build,
        CancellationToken cancellationToken)
    {
        var output = ToolArguments.Output(format, theme, width, height, _options);
        var table = ChartTable.Parse(data, _options.MaxRows);
        var built = build(table);
        var rendered = await _renderer.RenderAsync(built.Chart, chartType, title, output, cancellationToken);
        return ChartToolResult.Create(rendered, chartType, title, built, table);
    }

    /// <summary>For charts drawn from values passed directly (indicators), with no table to parse.</summary>
    public async Task<CallToolResult> RunAsync(
        string chartType,
        string? title,
        string? format,
        string? theme,
        int? width,
        int? height,
        Func<BuiltChart> build,
        CancellationToken cancellationToken)
    {
        var output = ToolArguments.Output(format, theme, width, height, _options);
        var built = build();
        var rendered = await _renderer.RenderAsync(built.Chart, chartType, title, output, cancellationToken);
        return ChartToolResult.Create(rendered, chartType, title, built, table: null);
    }
}
