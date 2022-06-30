using Robust.Shared.Utility;

namespace Robust.Packaging.AssetProcessing.Passes;

public sealed class AssetPassNormalizeEols : AssetPass
{
    private static readonly HashSet<string> TextFileExtensions = new()
    {
        // TODO: add more.
        "txt",
        "json",
        "yml",
        "html",
        "css",
        "js"
    };

    public override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        var extensionPeriod = file.Path.LastIndexOf('.');
        var extension = file.Path[(extensionPeriod + 1)..];
        if (!TextFileExtensions.Contains(extension))
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
