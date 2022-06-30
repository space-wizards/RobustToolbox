using Robust.Shared.Utility;

namespace Robust.Packaging.AssetProcessing.Passes;

public sealed class AssetPassPackageWriterIn : AssetPass, IPackageWriter
{
    public void WriteResource(string path, Stream stream)
    {
        SendFile(new AssetFileMemory(path, stream.CopyToArray()));
    }

    public void WriteResourceFromDisk(string path, string diskPath)
    {
        SendFile(new AssetFileDisk(path, diskPath));
    }
}

public sealed class AssetPassPackageWriterOut : AssetPass
{
    private readonly IPackageWriter _writer;

    public AssetPassPackageWriterOut(IPackageWriter writer)
    {
        _writer = writer;
    }

    public override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        switch (file)
        {
            case AssetFileDisk disk:
            {
                _writer.WriteResourceFromDisk(disk.Path, disk.DiskPath);
                break;
            }

            default:
            {
                using var stream = file.Open();
                _writer.WriteResource(file.Path, stream);
                break;
            }
        }

        return AssetFileAcceptResult.Consumed;
    }
}
