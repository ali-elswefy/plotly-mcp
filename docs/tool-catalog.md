# Tool catalog

Every tool, grouped by the `[McpServerToolType]` class that holds it (one file per tool). See
[plan.md](plan.md) for the data shape, the output formats and the conventions.

14 tools, all implemented: 13 chart tools (phases 1–2) and `create_dashboard` (phase 3). Every one outputs
HTML or an image.

## Common to every chart tool

**Parameters**

| Parameter | Type | Required | Meaning |
|---|---|---|---|
| `data` | `{ columns, rows }` | yes | The table to plot. `columns` are names or `{ "name": ... }` objects (a `run_select` result works as-is) |
| `title` | string | no | Chart title |
| `xTitle`, `yTitle` | string | no | Titles of the horizontal and vertical axes (also for horizontal bars). Default: the column names, or a description such as "Average Sales" or "Count" |
| `theme` | `light` \| `dark` | no | Default from `PlotlyTools:DefaultTheme` |
| `format` | `html` \| `png` \| `svg` | no | Output format. Default `html`. `png` / `svg` need a browser configured on the server |
| `width`, `height` | int | no | Image size in pixels; ignored for `html`, which resizes to fit. Default 1000×600 |

Not every chart has axes; pie, treemap, sunburst, funnel and indicator have no `xTitle` / `yTitle`.

**Result**: two content blocks.

1. **Summary** (`TextContentBlock`, JSON):

   | Field | Meaning |
   |---|---|
   | `chartType` | `bar`, `line`, … |
   | `format` | `html`, `png` or `svg` |
   | `title` | Omitted when none |
   | `traces`, `points` | How many traces and data points were plotted |
   | `truncated` | `true` when rows beyond `MaxRows` were dropped |
   | `filePath` | Only when the server saves output (`OutputDirectory` set) |
   | `note` | Omitted when none. Truncation, aggregation applied, series capped |

2. **The chart**:
   - `html`: `EmbeddedResourceBlock`, `text/html`, a complete responsive page.
   - `png`: `ImageContentBlock`, `image/png`.
   - `svg`: `ImageContentBlock`, `image/svg+xml`.

**Errors** (as `McpException`): unknown column (lists the available ones), a role that needs numbers given a
text column, no rows, more distinct `series` values than `MaxSeries`, `png` / `svg` when the server has no
browser configured ("use format html"), image rendering timed out.

---

## `BasicChartTools` (phase 1)

### `create_bar_chart`: Create a bar chart

Compare values across categories.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `x` | column | required | Category column, e.g. `"Region"` |
| `y` | column[] | required | One or more value columns; each becomes a bar series |
| `series` | column | none | Split one `y` column into a series per value of this column (instead of several `y` columns) |
| `orientation` | `vertical` \| `horizontal` | `vertical` | Horizontal suits long category names; categories read top-down |
| `barMode` | `grouped` \| `stacked` \| `percent` | `grouped` | `percent` stacks to 100% |
| `aggregate` | `sum` \| `avg` \| `count` \| `min` \| `max` | `sum` | How duplicate categories combine. Nulls are skipped; `count` counts non-empty values and works on text columns |
| `sort` | `none` \| `asc` \| `desc` | `none` | Sort categories by total value (`none` keeps first-seen order) |
| `showValues` | bool | `false` | Print values on the bars |

Categories always use a category axis, so years and codes aren't spread out as numbers. Several `y` columns and
a `series` column can't be combined. When duplicates are combined, the summary's `note` says so.

### `create_line_chart`: Create a line chart

Trends over an ordered axis, usually time.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `x` | column | required | Ordered column (date, number or ordered text) |
| `y` | column[] | required | One or more value columns |
| `series` | column | none | One line per value of this column |
| `markers` | bool | `false` | Show a marker at each point |
| `shape` | `linear` \| `spline` \| `step` | `linear` | Line interpolation |

Rows are sorted by `x` before plotting (numbers by value, text such as ISO dates alphabetically). Hovering shows
every series' value at that x.

### `create_area_chart`: Create an area chart

Like a line chart, filled, for totals over time and their make-up.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `x`, `y`, `series` | | | As in `create_line_chart` |
| `stacked` | bool | `true` | Stack the series |
| `normalize` | bool | `false` | Stack to 100% (needs `stacked`) |

### `create_scatter_chart`: Create a scatter or bubble chart

The relationship between two numeric columns.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `x`, `y` | column | required | Numeric columns |
| `series` | column | none | Colour points by this column |
| `size` | column | none | Numeric column for marker size; makes it a bubble chart. Bubble area is proportional to the value; the largest is 40 px across |
| `label` | column | none | Text shown on hover |
| `logX`, `logY` | bool | `false` | Logarithmic axes |

### `create_histogram`: Create a histogram

The distribution of one numeric column.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `x` | column | required | Numeric column |
| `series` | column | none | Overlaid histogram per value |
| `bins` | int 1–500 | auto | Number of bins |
| `normalize` | `count` \| `percent` \| `probability` \| `density` | `count` | Bar height |
| `cumulative` | bool | `false` | Cumulative distribution |

## `PartToWholeChartTools`

### `create_pie_chart`: Create a pie or donut chart (phase 1)

Shares of a whole, for a handful of categories.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `labels` | column | required | Category column |
| `values` | column | required | Numeric column |
| `aggregate` | as bar | `sum` | How duplicate labels combine |
| `hole` | number 0–0.9 | `0` | Above 0 makes a donut |
| `maxSlices` | int 2–50 | `8` | Smaller slices are merged into "Other" |
| `showPercent` | bool | `true` | Label slices with their percentage; `false` shows the value |

Negative totals are refused (with a pointer to `create_bar_chart`); slices whose values are all empty are left out.

### `create_funnel_chart`: Create a funnel chart (phase 2)

Values dropping through ordered stages.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `stage` | column | required | Stage column, in funnel order (the order stages first appear in the rows) |
| `values` | column | required | Numeric column |
| `series` | column | none | One funnel per value, side by side |
| `aggregate` | as bar | `sum` | |

Each stage shows its value and its share of the first stage.

### `create_treemap`: Create a treemap (phase 2)

Hierarchical shares as nested rectangles.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `path` | column[] | required | Hierarchy from outer to inner, e.g. `["Region", "Country", "City"]`. 1–6 levels |
| `values` | column | required | Numeric column |
| `aggregate` | as bar | `sum` | How rows with the same full path combine |
| `maxDepth` | int | all | Levels shown at once (1 to the number of path columns); clicking goes deeper |

- Leaves get the aggregated values; **every parent is the total of its children** (with `avg`, `min` or `max`
  the summary's `note` says so).
- The same label under different parents is a separate node (Oslo under North and under South).
- Leaves with no value are left out; negative totals are refused.

### `create_sunburst`: Create a sunburst chart (phase 2)

The same hierarchy as `create_treemap`, as rings, with the same parameters and rules. Slices are labelled with
their share of their parent.

## `DistributionChartTools` (phase 2)

### `create_box_plot`: Create a box or violin plot

Compare distributions across groups.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `y` | column | required | Numeric column |
| `x` | column | none | Group column; one box per value |
| `series` | column | none | Colour split within each group |
| `style` | `box` \| `violin` | `box` | Violins also show the shape of the distribution |
| `points` | `outliers` \| `all` \| `none` | `outliers` | Which raw points to draw |

Boxes show the mean as a dashed line; violins show an inner box and the mean. Pass raw values, one per row.

### `create_heatmap`: Create a heatmap

A value over two categorical axes. Takes long-format data (one row per cell).

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `x`, `y` | column | required | The two category columns |
| `z` | column | required | Numeric column for the colour |
| `aggregate` | as bar | `sum` | How duplicate cells combine |
| `colorScale` | string | `Viridis` | One of plotly.js's scales: Viridis, Cividis, Blues, Greens, Greys, Reds, YlGnBu, YlOrRd, RdBu, Portland, Picnic, Hot, Electric, Earth, Jet, Rainbow, Bluered, Blackbody (any case) |
| `showValues` | bool | `false` | Print values in the cells |

- Cells with no data stay empty. The first `y` category is at the top, like a table.
- Higher values always get the stronger colour: Blues, Greens, Greys, YlGnBu and YlOrRd are reversed, since
  plotly.js defines them dark at the low end.
- At most 40,000 cells (e.g. 200 × 200); beyond that the tool asks for fewer categories.

## `FinanceChartTools` (phase 2)

### `create_waterfall_chart`: Create a waterfall chart

How positive and negative changes add up to a total.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `labels` | column | required | Step names, in order |
| `values` | column | required | Change for each step |
| `measure` | column | none | Optional column of `relative` (a change; also used for empty cells), `total` (a running-total bar) or `absolute` (resets the running total), any case |
| `showTotal` | bool | `true` | Add a final total bar (when `measure` isn't given) |

Rows are drawn in order. Increases are green, decreases red, totals blue. Empty values count as 0.

### `create_indicator`: Create a KPI indicator or gauge

One number, optionally against a reference. Takes values, not `data`.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `value` | number | required | The number to show |
| `reference` | number | none | Shows the change against this value |
| `mode` | `number` \| `delta` \| `gauge` | `number` (`delta` when `reference` is set) | |
| `min`, `max` | number | `0`, auto | Gauge range. The automatic maximum is a round number about 10% above the value or reference (1,250,000 → 1,500,000) |
| `prefix`, `suffix` | string | none | e.g. `"$"`, `"%"` |
| `title` | string | none | Shown above the number (on the indicator itself, not in the corner) |

`mode: "delta"` needs `reference`. In a gauge, the reference is marked with a red line. No `xTitle` / `yTitle`.

## `DashboardTools` (phase 3)

### `create_dashboard`: Create a dashboard

Builds several charts in one call and lays them out together. Charts aren't stored between calls, so each
chart item carries its own definition: the chart `type` plus the same parameters that chart's tool takes.

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `title` | string | required | Dashboard title |
| `items` | list | required | Cards in order, see below. At most `MaxDashboardItems` (12) |
| `columns` | int 1–4 | `2` | Grid columns on wide screens; the HTML page drops to one column under 800 px |
| `description` | string | none | Text under the title |
| `theme` | `light` \| `dark` | default | Applies to the page and every chart |
| `format` | `html` \| `png` | `html` | No `svg`: the PNG is a screenshot of the page |
| `width` | int 100–4000 | `1400` | PNG width; the height follows the content. Ignored for `html` |

Item kinds:

```json
{ "type": "bar", "data": { "columns": [...], "rows": [...] }, "x": "Region", "y": ["Sales"], "columnSpan": 2 }
{ "type": "indicator", "value": 1250000, "reference": 1100000, "prefix": "$", "title": "Revenue" }
{ "heading": "Regional breakdown" }
{ "text": "Figures exclude returns.", "columnSpan": 2 }
```

- **Chart items:** `type` is one of `bar`, `line`, `area`, `scatter`, `histogram`, `box`, `heatmap`, `pie`,
  `funnel`, `treemap`, `sunburst`, `waterfall`, `indicator` (the summary's `chartType` names). They take every
  parameter of that chart's tool except `format`, `theme`, `width` and `height`, which the dashboard sets, and
  get the same defaults. Indicators take `value` / `reference` instead of `data`.
- **Card size:** `columnSpan` (default 1, capped at `columns`) and, for charts, `rowSpan` (1–3). A chart row is
  380 px tall. Headings run across the whole grid; text notes keep their own height.
- **Unknown parameters are refused** with the list the item takes, so a typo can't silently draw the wrong chart.
- `MaxRows` applies across all items together; when it runs out, later items are cut first and the `note` says which.
- **HTML:** one page, plotly.js loaded once, every chart interactive.
- **PNG:** the same page, screenshotted at `width` in the configured browser (plotly.js injected from the
  server, no internet needed). Headings, text and the description are included. About 1.5 s.

**Result**: the same two blocks as a chart tool. The summary has `title`, `format`, `items`, `charts`, `points`,
`truncated`, `filePath` and `note`. Notes from each chart are prefixed with their item, e.g.
`items[3] (pie): The 2 smallest slices were merged into 'Other'.`

**Errors**, each naming the item: an unknown `type` (lists the valid ones), a missing `type`/`heading`/`text`,
an unknown parameter, a chart's own errors (`items[2] (bar): Column 'Sales' (y) was not found…`), a value of the
wrong JSON type; more items than `MaxDashboardItems`; no chart items at all; `svg`.
