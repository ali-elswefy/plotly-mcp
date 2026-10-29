# PlotlyTools: plan

MCP tools that turn tabular data into Plotly charts and combine charts into dashboards. Every tool outputs
**HTML or an image**, nothing else. Charts are built with [Plotly.NET](https://plotly.net/).

It runs two ways:
- **Standalone MCP server** (`src/PlotlyTools.Server`), the main way since phase 2.
- **Headless library** (`src/PlotlyTools`): any other MCP server can host the same tools with `WithPlotlyTools`.

**Status: phase 3 done.** All 14 planned tools work end to end: 13 chart tools (HTML, PNG, SVG) and
`create_dashboard` (HTML, PNG). Phase 4 items are optional. Running and connecting: [README.md](../README.md).
Every change is logged in [memory.md](../memory.md).
Conventions for working in the repo are in [AGENTS.md](../AGENTS.md).

| Document | For |
|---|---|
| [plan.md](plan.md) | Architecture, conventions, phases, open decisions (this file) |
| [tool-catalog.md](tool-catalog.md) | Every planned tool: name, parameters, behaviour |

## Background

- `D:\plotly.rs` was considered first. Its build is Rust-only (`.rlib` files and a proc-macro DLL) and exports
  no C functions, so .NET can't call it without a hand-written FFI layer. Dropped in favour of Plotly.NET.
- Conventions, tool style and documentation layout follow `D:\sql_mcp` (AI_SqlMCP). Its `AGENTS.md` is the
  reference; the rules that apply here are copied into [Conventions](#conventions) below.

### What Plotly.NET gives us

Checked against Plotly.NET 5.1.0 and Plotly.NET.ImageExport 6.1.0:

- Building a chart returns a `GenericChart`: an in-memory object (one trace, or a `MultiChart` after
  `Chart.Combine`). It's changed with chained `With...` calls (`WithTitle`, `WithLayout`, `WithTemplate`…).
- **HTML:** `GenericChart.toChartHTML` returns a `<div>` + `<script>` fragment; `toEmbeddedHTML` returns a
  full page. plotly.js is referenced from the CDN unless the chart's display options embed it.
- **Images:** `Plotly.NET.ImageExport` renders PNG, JPG and SVG (`ToBase64PNGStringAsync`, `ToSVGStringAsync`,
  `SavePNGAsync`…) by loading the chart in headless Chromium through PuppeteerSharp.
  `PuppeteerSharpRendererOptions.localBrowserExecutablePath` points it at an installed Chrome or Edge instead
  of downloading Chromium. Each render starts its own browser: about 1.5 s per image. PNGs come back as a
  `data:image/png;base64,` URI, SVGs as plain markup.
- On this machine Chrome launches and Edge doesn't (`Failed to launch browser!`), most likely a managed-Edge
  policy. Use Chrome where Edge is locked down.
- The defaults aren't suitable as-is: every chart gets a fixed 600×600 size and a ~7 KB style template.
  Our shared styling replaces both.
- Figure JSON (`toFigureJson`) is used **only inside the library and its tests**, never returned by a tool.

## Tech stack

| Concern | Choice |
|---|---|
| Runtime | .NET 8 (decided), set once in `Directory.Build.props`. Library + ASP.NET Core server |
| Solution | Classic `.sln` (Format Version 12.00), not `.slnx`, so older Visual Studio versions can open it |
| MCP | `ModelContextProtocol` 2.3.0 in the library (no transport); `ModelContextProtocol.AspNetCore` 2.3.0 in the server. Same SDK version as sql_mcp |
| Charts | `Plotly.NET` 5.1.0. Traces are `Trace2D` objects filled with plotly.js attribute names (see [Building charts](#building-charts-one-builder-per-chart-type)). `Plotly.NET.CSharp` is used only by the tests |
| Images | `Plotly.NET.ImageExport` 6.1.0 (PuppeteerSharp 18), using a browser already installed on the host |
| Tests | xUnit 2.9.3, same packages as sql_mcp's test project |

## The standalone server

`src/PlotlyTools.Server` follows sql_mcp's `SqlMcp.Server`:

- ASP.NET Core, Streamable HTTP, **stateless**; `POST /mcp` and `GET /health`; `http://localhost:5200`.
- Configuration: `appsettings.json` (committed, empty browser path, saving off), then
  `appsettings.Local.json` (gitignored, machine-local) inserted just before environment variables, then the
  command line. Content root is the binary's folder, so it also runs as a Windows service.
- Server info `plotly-tools-mcp`; its instructions are `PlotlyToolsInfo.Instructions` (which tool for which
  question).
- The same `McpException` → tool error filter as sql_mcp.
- **No authentication**, like sql_mcp today: keep it on a trusted network.

Connect Claude Code: `claude mcp add --transport http plotly-tools http://localhost:5200/mcp`.

## How another host uses it (headless)

The server above is one host. Any other MCP server can reference the library (or the built DLL) and add one
line to its MCP registration:

```csharp
builder.Services
    .AddMcpServer(o => { /* host's server info */ })
    .WithHttpTransport(o => o.SessionMode = HttpServerSessionMode.Stateless)
    .WithToolsFromAssembly()                                    // the host's own tools
    .WithPlotlyTools(builder.Configuration.GetSection("PlotlyTools")); // this library's tools
```

`WithPlotlyTools` binds and validates `PlotlyToolsOptions`, registers the library's services (including the
image renderer and its browser path), and registers each `[McpServerToolType]` class explicitly with
`WithTools<T>()`, so the host doesn't need to scan this assembly.

`PlotlyToolsInfo.Instructions` can be appended to the host's own server instructions.

Expected by the library from the host:

- **Error filter.** Tools throw plain `McpException` for agent-facing errors (unknown column, bad parameter).
  The host turns those into tool errors, as sql_mcp's `AddCallToolFilter` already does.
- **Stateless is fine.** The library keeps nothing between calls: each call gets its data and returns the
  finished HTML or image.

## Design

### Data in: one tabular shape for every chart

Every chart tool takes the data the same way, as columns plus rows:

```json
{ "columns": ["Region", "Year", "Sales"], "rows": [["North", 2025, 120.5], ["South", 2025, 98.0]] }
```

- `columns` also accepts objects with a `name` property, so a sql_mcp `run_select` result can be passed
  straight through without reshaping.
- Tools then map columns to roles by name: `x`, `y`, `series`, `size`, `labels`, `values`, `path`…
- Unknown column names fail with a message that lists the available columns.
- Rows are capped at `MaxRows`; the summary says when data was cut off, like `run_select` does.
- Charts that need one value per category (bar, pie, funnel, treemap) aggregate duplicates with an
  `aggregate` parameter (`sum` by default). Agents should still aggregate in SQL where they can.

### Output: HTML or an image

Every chart tool takes `format`: `html` (default), `png` or `svg` (dashboards: `html` or `png`). It builds the
`GenericChart`, applies the shared styling, renders it in that format and returns two content blocks:

| Block | Content |
|---|---|
| Summary | `TextContentBlock` with a small JSON summary: chart type, title, traces, points, truncated, note, and the file path when files are saved |
| `html` | `EmbeddedResourceBlock` holding a `TextResourceContents` with `mimeType: text/html`: a complete page that loads plotly.js from the CDN |
| `png` | `ImageContentBlock`, `image/png`, base64. The agent can look at the chart it made |
| `svg` | `ImageContentBlock`, `image/svg+xml`, base64 |

- **Images** are rendered with plotly.js embedded in the page Chromium loads, so the server needs no internet
  access. `width` and `height` set the image size (default 1000×600).
- **Image output is off until configured.** It needs `BrowserExecutablePath` (an installed Chrome or Edge).
  Without it, `png` and `svg` fail with a message telling the agent to use `html`. PuppeteerSharp is never
  allowed to download Chromium.
- **Saving to disk is optional.** With `OutputDirectory` set, each result is also written there as
  `<id>.html` / `<id>.png` / `<id>.svg`, and the summary includes the path. By default nothing is written.
- **HTML pages are responsive:** no fixed size, and they resize with the window.
- **HTML pages are written by `HtmlPage`, not Plotly.NET's `toEmbeddedHTML`:** the page embeds the figure JSON in
  a script and calls `Plotly.newPlot`. That pins plotly.js 2.27.1 (the version Plotly.NET 5.1.0 targets), sets
  the responsive config, and keeps agent text out of the markup (`<` is escaped inside the JSON, titles are
  HTML-encoded).
- **Styling is applied by `ChartRenderer`**, the one step every chart passes through before output, so no tool
  can skip it.

### Building charts: one builder per chart type

Each chart type has a spec record (`BarChartSpec`: x, y, series, barMode…, in `Charts/ChartSpecs.cs`) and a
builder (`BarChartBuilder`) that turns the spec and a `ChartTable` into a `BuiltChart`: the `GenericChart` plus
trace and point counts and notes for the agent. Specs keep the tool parameters' names and leave defaults to the
builder (null = default), so dashboards can read the same specs from JSON and get the same defaults.

- **Tools** only bind parameters to a spec. `ChartToolRunner` does the rest for every tool: check the output
  parameters, parse the data, build, render, wrap the result. Dashboards call the same builders.
- **Traces are built directly** as Plotly.NET `Trace2D` objects with plotly.js attribute names (`x`, `y`,
  `stackgroup`, `histnorm`…), and combined with `GenericChart.ofTraceObjects`. Plotly.NET.CSharp's typed
  constructors need one .NET type per column, but agent data mixes numbers, text and nulls; plotly.js names are
  also the documented, stable ones.
- **Shared helpers:** `ChartBuilder` (traces, layout, axes, option parsing, splitting into series),
  `ChartTable.Groups` (series, capped at `MaxSeries`), `Aggregator` (sum/avg/count/min/max, nulls skipped as in
  SQL; count works on any column), `AxisOrder` (sorting line and area rows by x).

### Dashboards (phase 3)

There are no stored charts to point at, so `create_dashboard` takes the chart definitions themselves: a list
of items, each with a `type` (`bar`, `line`, …, `indicator`) and the same parameters that chart's tool takes.
Headings and text notes are items too.

- **Parsing (`DashboardParser`):** each chart item is read into the chart tool's own spec record and built by
  the same builder, so a chart looks the same alone and on a dashboard. Unknown parameters are refused with the
  list of valid ones (a typo would otherwise be ignored silently); `format`/`theme`/`width`/`height` are refused
  per item because the dashboard sets them. Errors name the item: `items[2] (bar): Column 'Sales' …`.
- **HTML (`DashboardPage`):** title and description, then a responsive CSS grid of cards (1–4 columns, one on
  screens under 800 px). Chart cards are 380 px per row they span; headings run across the grid; text notes
  keep their own height. Each chart is a `<div>` drawn with `Plotly.newPlot` from its styled figure; plotly.js
  loads once.
- **PNG — a screenshot of that same page**, not Plotly.NET's `Chart.Grid` as first planned. `ImageRenderer`
  opens the page in the configured browser through PuppeteerSharp, injects plotly.js from Plotly.NET's embedded
  copy (no internet needed), waits until every chart has drawn, and takes a full-page screenshot at `width`
  (default 1400 px; the height follows the content). Unlike a subplot grid this keeps headings, text, card
  layout and per-chart titles and legends, and mixed chart types (pies, indicators, treemaps next to bars) need
  no axis remapping. About 1.5 s.
- **No SVG for dashboards:** a page screenshot is a bitmap. Charts can still be SVGs one by one.
- Limits: `MaxDashboardItems` items, and `MaxRows` across all items together (later items are cut first, and
  the note says which).

### Styling

- One default look for every chart, set in a single `ChartStyle` class: a small template (colorway, font,
  margins, hover mode) replacing Plotly.NET's default, no fixed size, `responsive: true`, `displaylogo: false`.
- `theme` parameter on every tool: `light` (default) or `dark`.

## Conventions

Copied from sql_mcp's `AGENTS.md`. Phase 0 adds an `AGENTS.md` (and `CLAUDE.md` = `@AGENTS.md`) to this repo
with these rules.

### Partial classes (required)

- **Every class, record and struct is `partial`.** Interfaces and enums are exempt.
- **Split by concern** into `ClassName.Concern.cs` next to `ClassName.cs`:
  - `ClassName.cs` holds the XML summary, attributes (`[McpServerToolType]`), constructor, fields, shared helpers.
  - **One MCP tool per file:** `BasicChartTools.CreateBarChart.cs`.
  - Don't split a small single-concern class just to have more files; still mark it `partial`.
- **Primary constructors:** assign parameters to `private readonly` fields in `ClassName.cs`; other parts
  use the fields.
- Only `ClassName.cs` repeats attributes and the XML summary. Other parts start with a one-line comment
  saying what they contain (`// The create_bar_chart tool.`).

### Tool style

- Tool names are `snake_case` verbs: `create_bar_chart`, `create_dashboard`.
- `[McpServerTool(Name = ..., Title = ..., ReadOnly = true, Idempotent = true, OpenWorld = false)]`. The tools
  only render what they're given; saving to `OutputDirectory` is a side output, not a change agents rely on.
- Tools return `CallToolResult` with the content blocks described in [Output](#output-html-or-an-image).
- Tool classes are `internal` with a public constructor: the SDK creates them through that constructor, they
  take internal services (`ChartToolRunner`), and hosts never need the types because `WithPlotlyTools`
  registers them.
- Parameters most tools share take their `[Description]` from `ParameterDescriptions`, so every tool explains
  them the same way.
- The tool `[Description]` says what it returns, when to pick it over a similar chart, and which tool to call
  next. Each parameter `[Description]` gives an example value.
- Tool names are `const string` fields in `ClassName.cs` when the name is used elsewhere (logging, notes).
- Agent-facing errors are `McpException` with wording that tells the agent how to fix the call.
- Data shapes (specs, the summary) are records; empty optional fields are omitted with
  `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`.

### Style

- File-scoped namespaces matching the folder (`PlotlyTools.Charts`).
- Comments explain why, not what.
- Nullable reference types on; zero build warnings.

## Layout

```
Directory.Build.props        shared settings; target framework set here only
PlotlyTools.sln              classic .sln format; keep it that way when adding projects
README.md                    running the server, connecting clients, configuration
AGENTS.md, CLAUDE.md         conventions for contributors and AI agents
memory.md                    log of every change
docs/                        plan.md, tool-catalog.md
src/PlotlyTools.Server/      the standalone MCP server: Program.cs, appsettings.json (+ .Local.json, gitignored)
src/PlotlyTools/
  Charts/                    ChartDataInput + ChartTable (parsing, column roles, series groups), ChartStyle,
                             ChartTheme, ChartSpecs, one <Type>Builder per chart, ChartBuilder helpers,
                             Aggregator, AxisOrder
  Configuration/             PlotlyToolsOptions
  Dashboards/                DashboardCard, DashboardParser (+ .ChartTypes: item type → spec + builder),
                             DashboardPage (the grid page, for HTML and for the PNG screenshot)
  Rendering/                 ChartRenderer (+ .Dashboard; style → render → save), HtmlPage, ImageRenderer
                             (+ .Screenshot), PlotlyJs (embedded plotly.js), OutputFiles, OutputRequest,
                             RenderedChart
  Tools/                     BasicChartTools, DistributionChartTools, PartToWholeChartTools, FinanceChartTools,
                             DashboardTools (one file per tool); ChartToolRunner, ChartSummary /
                             DashboardSummary, ChartToolResult, ToolArguments, ParameterDescriptions
  McpServerBuilderExtensions.cs   WithPlotlyTools()
  PlotlyToolsInfo.cs         server instructions text, shared by every host
tests/PlotlyTools.Tests/     data parsing, builders, HTML rendering, tools; image tests when a browser is configured
```

## Configuration

Section `PlotlyTools`, bound by `WithPlotlyTools`:

| Setting | Default | Meaning |
|---|---|---|
| `BrowserExecutablePath` | empty | Installed Chrome or Edge used to render images. Empty turns `png` / `svg` off |
| `OutputDirectory` | empty | When set, results are also saved there. Relative paths are resolved from the app folder |
| `MaxRows` | `10000` | Rows read from `data` (per chart, and across a dashboard); the rest are dropped and the summary says so |
| `MaxSeries` | `50` | Distinct `series` values before the tool refuses and asks to filter |
| `MaxDashboardItems` | `12` | Items in one dashboard |
| `DefaultTheme` | `light` | Theme when a call doesn't pass one |
| `DefaultImageWidth`, `DefaultImageHeight` | `1000`, `600` | Image size when a call doesn't pass one |
| `RenderTimeoutSeconds` | `30` | Image rendering is cancelled after this |

## Phases

| Phase | Scope |
|---|---|
| **0** | Restructure to `src/` + `tests/`, `Directory.Build.props`, `AGENTS.md`, packages, `PlotlyToolsOptions`, `ChartData` parsing and column roles, `ChartStyle`, HTML and image renderers, optional file output, `WithPlotlyTools`, a dev host *(done)* |
| **1** | Core charts: `create_bar_chart`, `create_line_chart`, `create_area_chart`, `create_scatter_chart`, `create_pie_chart`, `create_histogram` *(done)* |
| **2** | Standalone server (`PlotlyTools.Server`, replacing the dev host) and more charts: `create_box_plot`, `create_heatmap`, `create_funnel_chart`, `create_waterfall_chart`, `create_treemap`, `create_sunburst`, `create_indicator` *(done)* |
| **3** | Dashboards: `create_dashboard` (HTML page, PNG screenshot of it) *(done)* |
| 4 | Later, if needed: authentication on the server, a warm browser for faster images, offline HTML (plotly.js embedded), JPG |

Each phase ends with: unit tests passing, zero warnings, the tools called through the server, and an entry
in `memory.md`.

## Testing

- **Unit tests** check what the builders make, not rendered pixels: parse `data`, call the builder, and
  assert on its figure JSON (trace types, x/y values, layout settings, aggregation results, truncation).
- **HTML:** the page holds one plot div per chart, loads plotly.js once, and has no fixed size.
- **Images:** render one PNG and one SVG and check the bytes are valid. These tests are skipped when no
  `BrowserExecutablePath` is configured on the test machine.
- Error cases: unknown column, wrong column type, too many series, empty data, image format without a browser.
- **Over MCP:** run the server on a spare port, call each tool over HTTP in each format, open the results,
  then stop the process so it doesn't hold the port or the build output (same rule as sql_mcp).

## Open decisions

Defaults chosen so work can start; change any of them before phase 0.

Decided (2026-10-28):
- Target framework is `net8.0`, matching sql_mcp, since a .NET 8 host can't reference a .NET 10 library.
- Tools output HTML or an image only. No figure JSON, no chart store; dashboards take chart definitions inline.
- Charts are built from core Plotly.NET objects (`Trace2D`, `Layout`), not Plotly.NET.CSharp (phase 1).
- The tools run as a standalone MCP server; the headless library stays, so other servers can host them (phase 2).
- Dashboard images are screenshots of the dashboard page (PNG only), not a `Chart.Grid` figure (phase 3).

| Decision | Default | Why / alternative |
|---|---|---|
| Image formats | `png` and `svg` | JPG is available too but adds nothing for charts |
| HTML in the result | The whole page inline (a few KB, plotly.js from the CDN) | A link to a saved file instead, once the host serves files over HTTP |
| Browser for images | An installed Chrome or Edge, set in config (Chrome verified; managed Edge may refuse to launch) | Letting PuppeteerSharp download Chromium (~150 MB) on first use |
