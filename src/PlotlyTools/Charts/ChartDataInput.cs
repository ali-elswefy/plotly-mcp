using System.ComponentModel;
using System.Text.Json;

namespace PlotlyTools.Charts;

/// <summary>
/// The table a chart tool plots, as agents pass it: column names plus rows. It's the shape sql_mcp's run_select
/// returns, so a query result can be charted without reshaping. <see cref="ChartTable"/> parses it.
/// </summary>
public sealed partial record ChartDataInput(
    [property: Description("Column names, e.g. [\"Region\", \"Sales\"]. Objects with a 'name' property also work, so run_select's columns can be passed as-is.")]
    IReadOnlyList<JsonElement>? Columns,
    [property: Description("Rows of values in column order, e.g. [[\"North\", 120.5], [\"South\", 98]].")]
    IReadOnlyList<IReadOnlyList<JsonElement>>? Rows);
