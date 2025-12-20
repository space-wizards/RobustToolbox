using System;
using System.IO;
using System.Threading.Tasks;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.UserInterface;

internal sealed class FileDialogManager(IClydeInternal clyde) : IFileDialogManager
{
    public async Task<Stream?> OpenFile(
        FileDialogFilters? filters = null,
        FileAccess access = FileAccess.ReadWrite,
        FileShare? share = null)
    {
        return (await OpenFile2(filters, access, share))?.File;
    }

    public async Task<OpenFileResult?> OpenFile2(FileDialogFilters? filters = null, FileAccess access = FileAccess.ReadWrite, FileShare? share = null)
    {
        if ((access & FileAccess.ReadWrite) != access)
            throw new ArgumentException("Invalid file access specified");

        var realShare = share ?? (access == FileAccess.Read ? FileShare.Read : FileShare.None);
        if ((realShare & (FileShare.ReadWrite | FileShare.Delete)) != realShare)
            throw new ArgumentException("Invalid file share specified");

        string? name;
        if (clyde.FileDialogImpl is { } clydeImpl)
            name = await clydeImpl.OpenFile(filters);
        else
            return null;

        if (name == null)
            return null;

        var handle = File.Open(name, FileMode.Open, access, realShare);
        return new OpenFileResult(handle, Path.GetFileName(name), alreadyExisted: true);
    }

    public async Task<(Stream, bool)?> SaveFile(
        FileDialogFilters? filters,
        bool truncate = true,
        FileAccess access = FileAccess.ReadWrite,
        FileShare share = FileShare.None)
    {
        var result = await SaveFile2(filters, truncate, access, share);
        if (result == null)
            return null;

        return (result.File, result.AlreadyExisted);
    }

    public async Task<OpenFileResult?> SaveFile2(
        FileDialogFilters? filters = null,
        bool truncate = true,
        FileAccess access = FileAccess.ReadWrite,
        FileShare share = FileShare.None)
    {
        if ((access & FileAccess.ReadWrite) != access)
            throw new ArgumentException("Invalid file access specified");

        if ((share & (FileShare.ReadWrite | FileShare.Delete)) != share)
            throw new ArgumentException("Invalid file share specified");

        string? name;
        if (clyde.FileDialogImpl is { } clydeImpl)
            name = await clydeImpl.SaveFile(filters);
        else
            return null;

        if (name == null)
            return null;

        Stream handle;
        var existed = true;
        try
        {
            handle = File.Open(name, truncate ? FileMode.Truncate : FileMode.Open, access, share);
        }
        catch (FileNotFoundException)
        {
            handle = File.Open(name, FileMode.Create, access, share);
            existed = false;
        }

        return new OpenFileResult(handle, Path.GetFileName(name), existed);
    }
}

public sealed class OpenFileCommand : LocalizedCommands
{
    public override string Command => "testopenfile";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var stream = await IoCManager.Resolve<IFileDialogManager>().OpenFile();
        stream?.Dispose();
    }
}
