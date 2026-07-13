using System.ComponentModel;
using System.Text;
using Enterprise.Agent.Core.Abstractions.Tools;
using Enterprise.Agent.Tools.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Enterprise.Agent.Tools;

/// <summary>
/// Sandboxed file access plugin. All operations are confined to a configured base directory;
/// path traversal outside the sandbox is rejected.
/// </summary>
public sealed class FileTool : IKernelPluginSource
{
    public const string PluginName = "File";

    private readonly FileToolOptions _options;
    private readonly ILogger<FileTool> _logger;

    public FileTool(IOptions<FileToolOptions> options, ILogger<FileTool> logger)
    {
        _options = options.Value;
        _logger = logger;
        Directory.CreateDirectory(_options.BasePath);
    }

    [KernelFunction("read_file"), Description("Reads a UTF-8 text file from the workspace and returns its content.")]
    public async Task<string> ReadFileAsync(
        [Description("Relative path within the workspace.")] string relativePath,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolve(relativePath, out var full))
        {
            return "Access denied: path is outside the workspace.";
        }

        if (!File.Exists(full))
        {
            return $"File '{relativePath}' does not exist.";
        }

        var info = new FileInfo(full);
        if (info.Length > _options.MaxReadBytes)
        {
            return $"File is too large to read ({info.Length} bytes; limit {_options.MaxReadBytes}).";
        }

        return await File.ReadAllTextAsync(full, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    [KernelFunction("write_file"), Description("Writes UTF-8 text to a file in the workspace, creating directories as needed.")]
    public async Task<string> WriteFileAsync(
        [Description("Relative path within the workspace.")] string relativePath,
        [Description("Text content to write.")] string content,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolve(relativePath, out var full))
        {
            return "Access denied: path is outside the workspace.";
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await File.WriteAllTextAsync(full, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            return $"Wrote {content.Length} characters to '{relativePath}'.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write file {Path}.", relativePath);
            return $"Failed to write '{relativePath}': {ex.Message}";
        }
    }

    [KernelFunction("list_files"), Description("Lists files under a relative directory in the workspace.")]
    public string ListFiles(
        [Description("Relative directory within the workspace (empty for root).")] string relativeDirectory = "")
    {
        if (!TryResolve(relativeDirectory.Length == 0 ? "." : relativeDirectory, out var full))
        {
            return "Access denied: path is outside the workspace.";
        }

        if (!Directory.Exists(full))
        {
            return $"Directory '{relativeDirectory}' does not exist.";
        }

        var entries = Directory.EnumerateFileSystemEntries(full)
            .Select(p => Path.GetRelativePath(_options.BasePath, p))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        return string.Join("\n", entries);
    }

    public KernelPlugin BuildPlugin() => KernelPluginFactory.CreateFromObject(this, PluginName);

    private bool TryResolve(string relativePath, out string fullPath)
    {
        var baseFull = Path.GetFullPath(_options.BasePath);
        fullPath = Path.GetFullPath(Path.Combine(baseFull, relativePath));
        return fullPath.StartsWith(baseFull, StringComparison.Ordinal);
    }
}
