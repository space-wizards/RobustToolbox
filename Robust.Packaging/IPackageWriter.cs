namespace Robust.Packaging;

/// <summary>
///
/// </summary>
/// <remarks>
/// Operations on this class are thread-safe.
/// </remarks>
public interface IPackageWriter
{
    void WriteResource(string path, Stream stream);

    void WriteResourceFromDisk(string path, string diskPath)
    {
        using var file = File.OpenRead(diskPath);
        WriteResource(path, file);
    }
}
