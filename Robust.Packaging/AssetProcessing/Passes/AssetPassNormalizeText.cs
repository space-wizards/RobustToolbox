using Robust.Shared.Utility;

namespace Robust.Packaging.AssetProcessing.Passes;

/// <summary>
/// Normalizes text files to make sure there is no BOM and EOLs are LF.
/// </summary>
public sealed class AssetPassNormalizeText : AssetPass
{
    private readonly HashSet<string> _textFileExtensions = new()
    {
        "txt",
        "json",
        "yml",
        "html",
        "css",
        "js",
        "ftl",
        "swsl",
        "toml",
        "svg",
        "xml"
    };

    /// <summary>
    /// The file extensions that are considered text files. Does not include the period.
    /// </summary>
    public ISet<string> TextFileExtensions => _textFileExtensions;

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        var extensionPeriod = file.Path.LastIndexOf('.');
        var extension = file.Path[(extensionPeriod + 1)..];
        if (!_textFileExtensions.Contains(extension))
            return AssetFileAcceptResult.Pass;

        // Console.WriteLine($"Normalizing EOLs: {file.Path}");

        using var fileStream = file.Open();
        using var sr = new StreamReader(fileStream);

        using var output = new MemoryStream();
        using var sw = new StreamWriter(output, EncodingHelpers.UTF8);

        while (sr.ReadLine() is { } line)
        {
            sw.Write(line);
            sw.Write("\n");
        }

        sw.Flush();

        SendFile(new AssetFileMemory(file.Path, output.ToArray()));

        return AssetFileAcceptResult.Consumed;
    }
}
