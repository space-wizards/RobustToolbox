namespace Robust.Packaging.AssetProcessing.Passes;

/// <summary>
/// Appends a prefix to file paths of passed-through files.
/// </summary>
public sealed class AssetPassPrefix : AssetPass
{
    public string Prefix { get; set; }

    public AssetPassPrefix(string prefix)
    {
        Prefix = prefix;
    }

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        var newPath = Prefix + file.Path;
        var newFile = file switch
        {
            AssetFileDisk disk => (AssetFile) new AssetFileDisk(newPath, disk.DiskPath),
            AssetFileMemory memory => new AssetFileMemory(newPath, memory.Memory),
            _ => throw new ArgumentOutOfRangeException(nameof(file))
        };

        SendFile(newFile);
        return AssetFileAcceptResult.Consumed;
    }
}
