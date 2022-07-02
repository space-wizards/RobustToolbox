namespace Robust.Packaging.AssetProcessing;

// TODO: Memory management strategies could be better.

/// <summary>
/// Represents a single file that is passed through the asset graph system.
/// </summary>
/// <seealso cref="AssetFileDisk"/>
/// <seealso cref="AssetFileMemory"/>
public abstract class AssetFile
{
    /// <summary>
    /// The destination path of the asset file in the VFS.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Open the file for reading.
    /// </summary>
    public abstract Stream Open();

    private protected AssetFile(string path)
    {
        Path = path;
    }
}

/// <summary>
/// A file of which the contents are backed by disk storage.
/// Avoids pulling files into memory immediately over <see cref="AssetFileMemory"/>.
/// </summary>
/// <remarks>
/// Files passed in must be considered to be immutable.
/// They should not change underneath our feet, even after the asset graph is done processing.
/// </remarks>
public sealed class AssetFileDisk : AssetFile
{
    public string DiskPath { get; }

    public AssetFileDisk(string path, string diskPath) : base(path)
    {
        DiskPath = diskPath;
    }

    public override Stream Open()
    {
        return File.OpenRead(DiskPath);
    }
}

/// <summary>
/// A file of which the contents are backed by memory.
/// </summary>
public sealed class AssetFileMemory : AssetFile
{
    public byte[] Memory { get; }

    public AssetFileMemory(string path, byte[] memory) : base(path)
    {
        Memory = memory;
    }

    public override Stream Open()
    {
        return new MemoryStream(Memory, false);
    }
}

