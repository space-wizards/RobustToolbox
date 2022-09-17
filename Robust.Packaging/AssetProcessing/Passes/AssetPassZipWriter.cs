using System.IO.Compression;

namespace Robust.Packaging.AssetProcessing.Passes;

/// <summary>
/// Writes incoming files into a <see cref="ZipArchive"/>.
/// </summary>
public sealed class AssetPassZipWriter : AssetPass
{
    public ZipArchive Archive { get; }

    public CompressionLevel Compression { get; set; } = CompressionLevel.Optimal;

    public AssetPassZipWriter(ZipArchive archive)
    {
        Archive = archive;
    }

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        switch (file)
        {
            case AssetFileDisk:
            {
                // Copy disk files out to a temporary buffer, so at least the read itself is parallel.
                // Avoid contention on the archive lock.
                using var fs = file.Open();
                using var ms = new MemoryStream((int)fs.Length);
                fs.CopyTo(ms);
                ms.Position = 0;

                lock (Archive)
                {
                    var entry = Archive.CreateEntry(file.Path, Compression);
                    using var entryStream = entry.Open();
                    ms.CopyTo(entryStream);
                }

                break;
            }

            default:
            {
                lock (Archive)
                {
                    var entry = Archive.CreateEntry(file.Path, Compression);
                    using var entryStream = entry.Open();
                    file.Open().CopyTo(entryStream);
                }
                break;
            }
        }

        return AssetFileAcceptResult.Consumed;
    }
}
