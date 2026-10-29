# AGENTS.md

Instructions for AI coding agents (and people) working in this repository. See [docs/plan.md](docs/plan.md) for
what the library does and why, and [docs/tool-catalog.md](docs/tool-catalog.md) for every planned tool.

## Commands

```bash
dotnet build PlotlyTools.sln
```

```bash
dotnet test PlotlyTools.sln
```

```bash
dotnet run --project src/PlotlyTools.Server
```

The server listens on `http://localhost:5200` (MCP endpoint `/mcp`, health `/health`). To run a second
instance for testing, override the port with the `Urls` environment variable or `--urls`.
`ASPNETCORE_URLS` does not work, because `Urls` in `appsettings.json` takes precedence over it.

## Two ways to run the tools

- **Standalone:** `src/PlotlyTools.Server` is an MCP server of its own (stateless HTTP, like sql_mcp).
- **Headless:** `src/PlotlyTools` is a class library. Any MCP server can host the same tools with
  `.WithPlotlyTools(configuration)` and `PlotlyToolsInfo.Instructions`. Keep everything tool-related in the
  library; the server project only hosts it (configuration, transport, error filter, health check).

Image output needs a browser. Put its path in `src/PlotlyTools.Server/appsettings.Local.json`
(gitignored), and set the `PLOTLYTOOLS_BROWSER` environment variable to the same path for the image tests:

```json
{ "PlotlyTools": { "BrowserExecutablePath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe" } }
```

## Change log

**Every change gets an entry in [memory.md](memory.md)**, newest first: the date, what was done, which files
changed, and any decision made.

## Solution file

`PlotlyTools.sln` stays in the classic format (Format Version 12.00) so older Visual Studio versions can open
it. The .NET 10 SDK creates `.slnx` by default: use `dotnet new sln --format sln`, and add projects with
`dotnet sln PlotlyTools.sln add`.

## C# conventions

### Partial classes (required)

- **Every class, record and struct is declared `partial`**: `public sealed partial class`,
  `internal static partial class`, `public sealed partial record`, `private readonly partial record struct`.
  Interfaces and enums are exempt.
- **Split a class by concern** into files named `ClassName.Concern.cs`, next to `ClassName.cs`:
  - `ClassName.cs` holds the declaration: XML summary, attributes (e.g. `[McpServerToolType]`), base types,
    constructor, fields, and the main entry points.
  - Each `ClassName.Concern.cs` file holds one concern: one MCP tool per file
    (`BasicChartTools.CreateBarChart.cs`), parsing (`ChartTable.Parse.cs`), typed reads (`ChartTable.Values.cs`).
  - Don't split a small class with a single concern just to have more files; still mark it `partial`.
- **Primary constructors:** their parameters are only in scope in the part that declares them. In a class
  split over several files, assign the parameters to `private readonly` fields in `ClassName.cs` and use the
  fields everywhere else.
- Only `ClassName.cs` repeats base types, attributes and the XML summary. Other parts are just
  `internal sealed partial class ClassName`, with a one-line comment saying what the part contains.

### Style

- File-scoped namespaces matching the folder (`PlotlyTools.Charts`).
- Records for data shapes. Shapes returned to agents omit empty optional fields with
  `[JsonIgnore(Condition = ...)]`.
- Comments explain why, not what. Keep the existing comment density.
- Nullable reference types are on. The build must stay at zero warnings.
- Only what a host or an agent needs is `public`: options, `WithPlotlyTools`, and the shapes agents send or
  receive (`ChartDataInput`, `ChartSummary`). Everything else is `internal`, including the tool classes (with a
  public constructor, which the MCP SDK uses); the test project sees it all through `InternalsVisibleTo`.

## Adding a chart tool

1. Add a spec record to `Charts/ChartSpecs.cs`, named after the tool parameters, all optional (null = default).
2. Add `Charts/<Type>Builder.cs`: spec + `ChartTable` → `BuiltChart`. Build traces with `ChartBuilder.NewTrace`
   and plotly.js attribute names; parse options with `ChartBuilder.Choose` / `InRange`.
3. Add `Tools/<Group>Tools.Create<Type>.cs`: parameters with descriptions (shared ones from
   `ParameterDescriptions`), then `_runner.RunAsync(...)` with the builder.
4. Register a new tool class in `WithPlotlyTools`; add builder tests on the figure JSON and update
   `ChartToolsTests.Registers_...`.
5. Add the type to `Dashboards/DashboardParser.ChartTypes.cs` (name = the tool's chartType), so dashboards can
   use it, and to the `items` description in `DashboardTools.CreateDashboard.cs`.
6. Update `docs/tool-catalog.md`, `PlotlyToolsInfo.Instructions` (which tool for which question) and `memory.md`.

## Tool rules (do not weaken)

1. **Tools output HTML or an image, nothing else.** Every chart or dashboard tool returns a JSON summary block
   plus the chart as an HTML page (`text/html` resource) or an image (`image/png`, `image/svg+xml`). Figure JSON
   is internal: builders and tests use it, tools never return it.
2. **Every chart goes through `ChartStyle`** (in `ChartRenderer`, for single charts and dashboards alike), so all
   charts share one look and none has a fixed size. Dashboard charts use the chart tools' builders, never their own.
3. **Image output fails closed.** Without `PlotlyTools:BrowserExecutablePath`, `png` and `svg` are refused
   with a message pointing the agent at `html`. PuppeteerSharp must never download Chromium.
4. **Data from agents is untrusted.** It's written into HTML pages: keep it inside the JSON that the page
   script reads (with `</` escaped), and HTML-encode anything placed in markup.
5. **Agent-facing errors are `McpException`** with wording that tells the agent how to fix the call (which
   column, which row, what's available). The host turns them into tool errors.
6. **No state between calls.** Each call gets its data and returns finished output, so any host instance can
   serve any call.

## Tests

- Builders: assert on the figure JSON (trace types, values, layout), not on rendered pixels.
- HTML: the page draws every chart, loads plotly.js once, and has no fixed size.
- Image tests use `[BrowserFact]` and are skipped when `PLOTLYTOOLS_BROWSER` isn't set.
- For tool changes, run the server on a spare port and call the tools over HTTP. Stop the process
  afterwards, so it doesn't keep holding the port and the build output.
