using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_waterfall_chart tool.
internal sealed partial class FinanceChartTools
{
    [McpServerTool(Name = "create_waterfall_chart", Title = "Create a waterfall chart", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws a waterfall chart: positive and negative steps that add up to a total, and returns it as an HTML page " +
        "or an image, after a short JSON summary. Use it to explain how a figure moved from one value to another " +
        "(opening balance, income, costs, closing balance). Rows are drawn in order; a closing total is added unless " +
        "a measure column says which rows are totals.")]
    public Task<CallToolResult> CreateWaterfallChartAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description("Step name column, in order, e.g. 'Item'.")] string labels,
        [Description("Numeric column with each step's change (or a total row's value), e.g. 'Amount'.")] string values,
        [Description("Optional column saying what each row is: 'relative' (a change, the default), 'total' (a running total bar) or 'absolute' (resets the running total).")] string? measure = null,
        [Description("Add a closing total bar. Default true; ignored when a measure column is given.")] bool? showTotal = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.XTitle)] string? xTitle = null,
        [Description(ParameterDescriptions.YTitle)] string? yTitle = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new WaterfallChartSpec(labels, values, measure, showTotal, title, xTitle, yTitle);
        return _runner.RunAsync(
            "waterfall", data, title, format, theme, width, height,
            table => WaterfallChartBuilder.Build(spec, table),
            cancellationToken);
    }
}
