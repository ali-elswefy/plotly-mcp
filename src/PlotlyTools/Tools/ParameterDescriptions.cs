namespace PlotlyTools.Tools;

/// <summary>Descriptions of the parameters most chart tools share, so every tool explains them the same way.</summary>
internal static partial class ParameterDescriptions
{
    public const string Data =
        "The table to plot: {\"columns\": [...], \"rows\": [[...], ...]}. A run_select result can be passed as-is. " +
        "Aggregate in SQL first where you can: rows beyond the server's limit are dropped.";

    public const string Series = "Optional column to split the data by: one series (colour) per distinct value, e.g. 'Region'.";

    public const string Title = "Chart title, e.g. 'Sales by region'.";

    public const string XTitle = "Title of the horizontal axis. Defaults to the column name.";

    public const string YTitle = "Title of the vertical axis. Defaults to the column name.";

    public const string Theme = "'light' or 'dark'. Defaults to the server's theme.";

    public const string Format =
        "'html' (default): an interactive page. 'png' or 'svg': an image you can look at; only available when the " +
        "server has a browser configured.";

    public const string Width = "Image width in pixels (100-4000), for png and svg. The html page resizes to fit instead.";

    public const string Height = "Image height in pixels (100-4000), for png and svg.";

    public const string Aggregate =
        "How rows that share a category combine: 'sum' (default), 'avg', 'count', 'min' or 'max'. 'count' counts non-empty values.";
}
