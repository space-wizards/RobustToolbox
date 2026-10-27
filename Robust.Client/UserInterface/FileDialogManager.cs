using System;
using System.IO;
using System.Threading.Tasks;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.UserInterface;

internal sealed class FileDialogManager(IClydeInternal clyde) : IFileDialogManager
{
    private static FileShare Validate(FileAccess access, FileShare? share)
    {
        if ((access & FileAccess.ReadWrite) != access)
            throw new ArgumentException("Invalid file access specified");

        var realShare = share ?? (access == FileAccess.Read ? FileShare.Read : FileShare.None);

        if ((realShare & (FileShare.ReadWrite | FileShare.Delete)) != realShare)
            throw new ArgumentException("Invalid file share specified");

        return realShare;
    }

    private async Task<string?> Prompt(bool isSave, FileDialogFilters? filters)
    {
        if (clyde.FileDialogImpl is { } impl)
            return isSave ? await impl.SaveFile(filters) : await impl.OpenFile(filters);

        return null;
    }

    public async Task<(Stream?, string?)> GetFileAndName(
        FileDialogFilters? filters = null,
        FileAccess access = FileAccess.ReadWrite,
        FileShare? share = null)
    {
        var name = await Prompt(false, filters);

        if (name == null) return (null, null);

        var path = Path.GetFileName(name);
        return (File.Open(name, FileMode.Open, access, Validate(access, share)), path);
    }

    public async Task<string?> GetName(
        FileDialogFilters? filters = null,
        FileAccess access = FileAccess.ReadWrite,
        FileShare? share = null)
    {
        Validate(access, share);
        var name = await Prompt(false, filters);

        return name == null ? null : Path.GetFileName(name);
    }

    public async Task<Stream?> OpenFile(
        FileDialogFilters? filters = null,
        FileAccess access = FileAccess.ReadWrite,
        FileShare? share = null)
    {
        var realShare = Validate(access, share);
        var name = await Prompt(false, filters);

        return name == null ? null : File.Open(name, FileMode.Open, access, realShare);
    }

    public async Task<(Stream, bool)?> SaveFile(
        FileDialogFilters? filters,
        bool truncate = true,
        FileAccess access = FileAccess.ReadWrite,
        FileShare share = FileShare.None)
    {
        Validate(access, share);
        var name = await Prompt(true, filters);

        if (name == null) return null;

        try
        {
            return (File.Open(name, truncate ? FileMode.Truncate : FileMode.Open, access, share), true);
        }
        catch (FileNotFoundException)
        {
            return (File.Open(name, FileMode.Create, access, share), false);
        }
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
