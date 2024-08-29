using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Console;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "CommentTypo")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal sealed class FileDialogManager : IFileDialogManager
    {
        // Uses nativefiledialog to open the file dialogs cross platform.
        // On Linux, if the kdialog command is found, it will be used instead.
        // TODO: Should we maybe try to avoid running kdialog if the DE isn't KDE?
        [Dependency] private readonly IClydeInternal _clyde = default!;

        private bool _kDialogAvailable;
        private bool _checkedKDialogAvailable;

        public async Task<Stream?> OpenFile(FileDialogFilters? filters = null)
        {
            var name = await GetOpenFileName(filters);
            if (name == null)
            {
                return null;
            }

            return File.Open(name, FileMode.Open);
        }

        private async Task<string?> GetOpenFileName(FileDialogFilters? filters)
        {
            if (await IsKDialogAvailable())
            {
                return await OpenFileKDialog(filters);
            }

            return await OpenFileNfd(filters);
        }

        public async Task<(Stream, bool)?> SaveFile(FileDialogFilters? filters, bool truncate = true)
        {
            var name = await GetSaveFileName(filters);
            if (name == null)
            {
                return null;
            }

            try
            {
                return (File.Open(name, truncate ? FileMode.Truncate : FileMode.Open), true);
            }
            catch (FileNotFoundException)
            {
                return (File.Open(name, FileMode.Create), false);
            }
        }

        private async Task<string?> GetSaveFileName(FileDialogFilters? filters)
        {
            if (await IsKDialogAvailable())
            {
                return await SaveFileKDialog(filters);
            }

            return await SaveFileNfd(filters);
        }

        private unsafe Task<string?> OpenFileNfd(FileDialogFilters? filters)
        {
            // Have to run it in the thread pool to avoid blocking the main thread.
            return RunAsyncMaybe(() =>
            {
                byte* outPath;

                var filterPtr = FormatFiltersNfd(filters);

                sw_nfdresult result;

                try
                {
                    result = sw_NFD_OpenDialog((byte*)filterPtr, null, &outPath);
                }
                finally
                {
                    if (filterPtr != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(filterPtr);
                    }
                }

                return HandleNfdResult(result, outPath);
            });
        }

        private static IntPtr FormatFiltersNfd(FileDialogFilters? filters)
        {
            if (filters != null)
            {
                var filterString = string.Join(';', filters.Groups.Select(f => string.Join(',', f.Extensions)));

                return Marshal.StringToCoTaskMemUTF8(filterString);
            }

            return IntPtr.Zero;
        }

        private unsafe Task<string?> SaveFileNfd(FileDialogFilters? filters)
        {
            // Have to run it in the thread pool to avoid blocking the main thread.
            return RunAsyncMaybe(() =>
            {
                byte* outPath;

                var filterPtr = FormatFiltersNfd(filters);

                sw_nfdresult result;
                try
                {
                    result = sw_NFD_SaveDialog((byte*) filterPtr, null, &outPath);
                }
                finally
                {
                    if (filterPtr != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(filterPtr);
                    }
                }

                return HandleNfdResult(result, outPath);
            });
        }

        /*
        private unsafe Task<string?> OpenFolderNfd()
        {
            // Have to run it in the thread pool to avoid blocking the main thread.
            return RunAsyncMaybe(() =>
            {
                byte* outPath;

                var result = sw_NFD_PickFolder(null, &outPath);

                return HandleNfdResult(result, outPath);
            });
        }
        */

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Task<string?> RunAsyncMaybe(Func<string?> action)
        {
            if (OperatingSystem.IsMacOS())
            {
                // macOS seems pretty annoying about having the file dialog opened from the main windowing thread.
                // So we are forced to execute this synchronously on the main windowing thread.
                // nativefiledialog doesn't provide any form of async API, so this WILL lock up half the client.
                var tcs = new TaskCompletionSource<string?>();
                _clyde.RunOnWindowThread(() => tcs.SetResult(action()));

                return tcs.Task;
            }
            else
            {
                // Luckily, GTK Linux and COM Windows are both happily threaded. Yay!
                // * Actual attempts to have multiple file dialogs up at the same time, and the resulting crashes,
                // have shown that at least for GTK+ (Linux), just because it can handle being on any thread doesn't mean it handle being on two at the same time.
                // Testing system was Ubuntu 20.04.
                // COM on Windows might handle this, but honestly, who exactly wants to risk it?
                // In particular this could very well be an swnfd issue.
                return Task.Run(() =>
                {
                    lock (this)
                    {
                        return action();
                    }
                });
            }
        }

        private static unsafe string? HandleNfdResult(sw_nfdresult result, byte* outPath)
        {
            switch (result)
            {
                case sw_nfdresult.SW_NFD_ERROR:
                    var errPtr = sw_NFD_GetError();
                    throw new Exception(Marshal.PtrToStringUTF8((IntPtr) errPtr));

                case sw_nfdresult.SW_NFD_OKAY:
                    var str = Marshal.PtrToStringUTF8((IntPtr) outPath)!;

                    sw_NFD_Free(outPath);
                    return str;

                case sw_nfdresult.SW_NFD_CANCEL:
                    return null;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task CheckKDialogSupport()
        {
            var currentDesktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
            if (currentDesktop == null || !currentDesktop.Contains("KDE"))
            {
                return;
            }

            try
            {
                var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "kdialog",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    });

                if (process == null)
                {
                    _kDialogAvailable = false;
                    return;
                }

                await process.WaitForExitAsync();
                _kDialogAvailable = process.ExitCode == 0;

                if (_kDialogAvailable)
                {
                    Logger.DebugS("filedialog", "kdialog available.");
                }
            }
            catch
            {
                _kDialogAvailable = false;
            }
        }

        private Task<string?> OpenFileKDialog(FileDialogFilters? filters)
        {
            var filtersFormatted = FormatFiltersKDialog(filters);

            return RunKDialog("--getopenfilename", Environment.GetEnvironmentVariable("HOME")!, filtersFormatted);
        }

        private static string FormatFiltersKDialog(FileDialogFilters? filters)
        {
            var sb = new StringBuilder();

            if (filters != null && filters.Groups.Count != 0)
            {
                var first = true;
                foreach (var group in filters.Groups)
                {
                    if (!first)
                    {
                        sb.Append('|');
                    }

                    foreach (var extension in @group.Extensions)
                    {
                        sb.AppendFormat(".{0} ", extension);
                    }

                    sb.Append('(');

                    foreach (var extension in @group.Extensions)
                    {
                        sb.AppendFormat("*.{0} ", extension);
                    }

                    sb.Append(')');

                    first = false;
                }

                sb.Append("| All Files (*)");
            }

            return sb.ToString();
        }

        private Task<string?> SaveFileKDialog(FileDialogFilters? filters)
        {
            var filtersFormatted = FormatFiltersKDialog(filters);

            return RunKDialog("--getsavefilename", Environment.GetEnvironmentVariable("HOME")!, filtersFormatted);
        }

        /*
        private Task<string?> OpenFolderKDialog()
        {
            return RunKDialog("--getexistingdirectory");
        }
        */

        private async Task<string?> RunKDialog(params string[] options)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "kdialog",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                StandardOutputEncoding = EncodingHelpers.UTF8
            };

            foreach (var option in options)
            {
                startInfo.ArgumentList.Add(option);
            }

            if (_clyde.GetX11WindowId() is { } id)
            {
                startInfo.ArgumentList.Add("--attach");
                startInfo.ArgumentList.Add(id.ToString());
            }

            var process = Process.Start(startInfo);

            DebugTools.AssertNotNull(process);

            await process!.WaitForExitAsync();

            // Cancel hit.
            if (process.ExitCode == 1)
            {
                return null;
            }

            return (await process.StandardOutput.ReadLineAsync())?.Trim();
        }

        private async Task<bool> IsKDialogAvailable()
        {
            if (!OperatingSystem.IsLinux())
                return false;

            if (!_checkedKDialogAvailable)
            {
                await CheckKDialogSupport();
                _checkedKDialogAvailable = true;
            }

            return _kDialogAvailable;
        }

        [DllImport("swnfd.dll")]
        private static extern unsafe byte* sw_NFD_GetError();

        [DllImport("swnfd.dll")]
        private static extern unsafe sw_nfdresult
            sw_NFD_OpenDialog(byte* filterList, byte* defaultPath, byte** outPath);

        [DllImport("swnfd.dll")]
        private static extern unsafe sw_nfdresult
            sw_NFD_SaveDialog(byte* filterList, byte* defaultPath, byte** outPath);

        /*
        [DllImport("swnfd.dll")]
        private static extern unsafe sw_nfdresult
            sw_NFD_PickFolder(byte* defaultPath, byte** outPath);
            */

        [DllImport("swnfd.dll")]
        private static extern unsafe void sw_NFD_Free(void* ptr);

        private enum sw_nfdresult
        {
            SW_NFD_ERROR,
            SW_NFD_OKAY,
            SW_NFD_CANCEL,
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
