using Microsoft.Extensions.Options;
using PlotlyTools.Configuration;

namespace PlotlyTools.Rendering;

/// <summary>Saves rendered output to OutputDirectory when one is configured; otherwise saves nothing.</summary>
internal sealed partial class OutputFiles
{
    private readonly string? _directory;

    public OutputFiles(IOptions<PlotlyToolsOptions> options)
    {
        var directory = options.Value.OutputDirectory;
        _directory = string.IsNullOrWhiteSpace(directory) ? null : Path.GetFullPath(directory, AppContext.BaseDirectory);
    }

    public bool IsEnabled => _directory is not null;

    /// <summary>A name that sorts by time and can't collide between calls, e.g. "bar-20261028-142501-3f9c2a1b.html".</summary>
    public static string NewFileName(string kind, OutputRequest output) =>
        $"{kind}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}.{output.FileExtension}";

    /// <summary>Writes the file and returns its full path, or returns null when saving is off.</summary>
    public async Task<string?> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken)
    {
        if (_directory is null)
        {
            return null;
        }

        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, fileName);
        await File.WriteAllBytesAsync(path, content, cancellationToken);
        return path;
    }
}
