using Robust.Shared.Utility;

namespace Robust.Packaging.AssetProcessing.Passes;

public sealed class AssetPassMergeTextDirectories : AssetPass
{
    private readonly ResPath _prefixPath;
    private readonly string _extension;
    private readonly Func<string, string>? _formatterHead;
    private readonly Func<string, string>? _formatterTail;

    private readonly Dictionary<ResPath, DirectoryDatum> _data = new();

    public AssetPassMergeTextDirectories(
        string prefixPath,
        string extension,
        Func<string, string>? formatterHead = null,
        Func<string, string>? formatterTail = null)
    {
        _prefixPath = new ResPath(prefixPath);
        _extension = extension;
        _formatterHead = formatterHead;
        _formatterTail = formatterTail;
    }

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        var resPath = new ResPath(file.Path);
        if (!resPath.TryRelativeTo(_prefixPath, out _))
            return AssetFileAcceptResult.Pass;

        if (resPath.Extension != _extension)
            return AssetFileAcceptResult.Pass;

        var directory = resPath.Directory;
        lock (_data)
        {
            var datum = _data.GetOrNew(directory);
            datum.Files.Add(file);
        }

        return AssetFileAcceptResult.Consumed;
    }

    protected override void AcceptFinished()
    {
        RunJob(() =>
        {
            lock (_data)
            {
                var ms = new MemoryStream();
                var writer = new StreamWriter(ms, EncodingHelpers.UTF8);

                foreach (var (directory, datum) in _data)
                {
                    ms.Position = 0;
                    var mergedFile = directory / $"__merged.{_extension}";
                    WriteForDatum(datum, writer);
                    writer.Flush();

                    SendFileFromMemory(mergedFile.ToString(), ms.GetBuffer()[..(int)ms.Position]);
                }

                _data.Clear();
            }
        });
    }

    private void WriteForDatum(DirectoryDatum datum, StreamWriter writer)
    {
        foreach (var file in datum.Files.OrderBy(f => f.Path, StringComparer.Ordinal))
        {
            if (_formatterHead != null)
            {
                writer.Write(_formatterHead(file.Path));
                writer.Write('\n');
            }

            using var stream = file.Open();
            using var reader = new StreamReader(stream);
            while (reader.ReadLine() is { } line)
            {
                writer.Write(line);
                writer.Write('\n');
            }

            if (_formatterTail != null)
            {
                writer.Write(_formatterTail(file.Path));
                writer.Write('\n');
            }
        }
    }

    private sealed class DirectoryDatum
    {
        public readonly List<AssetFile> Files = [];
    }
}
