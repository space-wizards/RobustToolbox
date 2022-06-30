namespace Robust.Packaging.AssetProcessing;

// TODO: Memory management strategies need revising.

public abstract class AssetFile
{
    public string Path { get; }

    public abstract Stream Open();

    protected AssetFile(string path)
    {
        Path = path;
    }
}

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

public sealed class AssetFileMemory : AssetFile
{
    private readonly byte[] _memory;

    public AssetFileMemory(string path, byte[] memory) : base(path)
    {
        _memory = memory;
    }

    public override Stream Open()
    {
        return new MemoryStream(_memory, false);
    }
}
