using System;
using System.IO;
using System.Threading.Tasks;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.UserInterface
{
    internal sealed class FileDialogManager : IFileDialogManager
    {
        [Dependency] private readonly IClydeInternal _clyde = default!;

        public async Task<Stream?> OpenFile(
            FileDialogFilters? filters = null,
            FileAccess access = FileAccess.ReadWrite,
            FileShare? share = null)
        {
            if ((access & FileAccess.ReadWrite) != access)
                throw new ArgumentException("Invalid file access specified");

            var realShare = share ?? (access == FileAccess.Read ? FileShare.Read : FileShare.None);
            if ((realShare & (FileShare.ReadWrite | FileShare.Delete)) != realShare)
                throw new ArgumentException("Invalid file share specified");

            string? name;
            if (_clyde.FileDialogImpl is { } clydeImpl)
                name = await clydeImpl.OpenFile(filters);
            else
                throw new NotSupportedException();

            if (name == null)
                return null;

            return File.Open(name, FileMode.Open, access, realShare);
        }

        public async Task<(Stream, bool)?> SaveFile(
            FileDialogFilters? filters,
            bool truncate = true,
            FileAccess access = FileAccess.ReadWrite,
            FileShare share = FileShare.None)
        {
            if ((access & FileAccess.ReadWrite) != access)
                throw new ArgumentException("Invalid file access specified");

            if ((share & (FileShare.ReadWrite | FileShare.Delete)) != share)
                throw new ArgumentException("Invalid file share specified");

            string? name;
            if (_clyde.FileDialogImpl is { } clydeImpl)
                name = await clydeImpl.SaveFile(filters);
            else
                throw new NotSupportedException();

            if (name == null)
                return null;

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
}
