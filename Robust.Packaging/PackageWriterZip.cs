using System.IO.Compression;

namespace Robust.Packaging;

public sealed class PackageWriterZip : IPackageWriter
{
    public ZipArchive Archive { get; }

    public PackageWriterZip(ZipArchive archive)
    {
        Archive = archive;
    }

    public void WriteResource(string path, Stream stream)
    {
        lock (Archive)
        {
            var entry = Archive.CreateEntry(path);
            using var entryStream = entry.Open();
            stream.CopyTo(entryStream);
        }
    }

    public void WriteResourceFromDisk(string path, string diskPath)
    {
        lock (Archive)
        {
            Archive.CreateEntryFromFile(diskPath, path);
        }
    }
}
