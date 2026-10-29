namespace PlotlyTools;

/// <summary>
/// Text a host gives its MCP clients about these tools. The standalone server uses it as its instructions; a host
/// that adds the tools headless can append it to its own.
/// </summary>
public static partial class PlotlyToolsInfo
{
    public const string Instructions =
        "Draws charts from tabular data and returns each as an interactive HTML page or a PNG/SVG image, after a " +
        "short JSON summary. Pass data as {\"columns\": [...], \"rows\": [[...], ...]}; a SQL run_select result works " +
        "as-is. Aggregate in SQL first where you can. Pick the tool by the question: compare categories with " +
        "create_bar_chart; trends over time with create_line_chart or create_area_chart; how two numbers relate with " +
        "create_scatter_chart; distributions with create_histogram or create_box_plot; parts of a whole with " +
        "create_pie_chart, create_treemap, create_sunburst or create_funnel_chart; a value across two categories " +
        "with create_heatmap; changes adding up to a total with create_waterfall_chart; one KPI number with " +
        "create_indicator. To put several of these on one page, with headings and notes, use create_dashboard: " +
        "every chart goes in that one call. Use format 'png' when you want to look at the chart yourself.";
}
