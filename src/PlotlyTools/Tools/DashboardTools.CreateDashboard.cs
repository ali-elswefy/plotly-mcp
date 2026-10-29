using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Dashboards;
using PlotlyTools.Rendering;

namespace PlotlyTools.Tools;

// The create_dashboard tool.
internal sealed partial class DashboardTools
{
    private const int DefaultColumns = 2;

    [McpServerTool(Name = "create_dashboard", Title = "Create a dashboard", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Builds several charts in one call and lays them out as a dashboard: a title, then a grid of cards with " +
        "optional headings and text notes. Returns it as an HTML page or a PNG image, after a short JSON summary. " +
        "Each chart item is {\"type\": <chart>, \"data\": {columns, rows}, ...} with the same parameters as that " +
        "chart's create_* tool (except format, theme, width and height, which the dashboard sets), e.g. " +
        "{\"type\": \"bar\", \"data\": {...}, \"x\": \"Region\", \"y\": [\"Sales\"], \"title\": \"Sales by region\"}. " +
        "Indicator items take value/reference instead of data. Charts aren't stored between calls, so every chart " +
        "goes in this one call.")]
    public async Task<CallToolResult> CreateDashboardAsync(
        [Description("Dashboard title, e.g. 'Sales overview, Q3'.")] string title,
        [Description(
            "Cards in order. Chart: {\"type\": \"bar\" | \"line\" | \"area\" | \"scatter\" | \"histogram\" | \"box\" | " +
            "\"heatmap\" | \"pie\" | \"funnel\" | \"treemap\" | \"sunburst\" | \"waterfall\" | \"indicator\", \"data\": {...}, " +
            "...that chart's parameters, \"columnSpan\": 1, \"rowSpan\": 1}. Heading across the grid: {\"heading\": \"Regions\"}. " +
            "Text note: {\"text\": \"Figures exclude returns.\", \"columnSpan\": 2}.")]
        JsonElement[] items,
        [Description("Grid columns on wide screens, 1-4. Default 2. The HTML page drops to one column on narrow screens.")] int? columns = null,
        [Description("Optional text under the title.")] string? description = null,
        [Description(ParameterDescriptions.Theme + " Applies to every chart.")] string? theme = null,
        [Description("'html' (default): an interactive page. 'png': an image of the whole dashboard, only available when the server has a browser configured.")] string? format = null,
        [Description("Image width in pixels (100-4000), for png. Default 1400; the height follows the content.")] int? width = null,
        CancellationToken cancellationToken = default)
    {
        var output = ToolArguments.Output(format, theme, width ?? DashboardPage.DefaultWidth, null, _options);
        if (output.Format == OutputFormat.Svg)
        {
            throw new McpException("Dashboards come as 'html' or 'png'. For an SVG, create the charts one by one.");
        }

        var gridColumns = columns switch
        {
            null => DefaultColumns,
            >= 1 and <= DashboardParser.MaxColumns => columns.Value,
            _ => throw new McpException($"columns must be between 1 and {DashboardParser.MaxColumns}."),
        };

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new McpException("Give the dashboard a title, e.g. 'Sales overview'.");
        }

        var dashboard = DashboardParser.Parse(items, gridColumns, _options);
        var page = await _renderer.RenderDashboardAsync(title, description, gridColumns, dashboard, output, cancellationToken);
        return ChartToolResult.CreateDashboard(page, title, dashboard);
    }
}
