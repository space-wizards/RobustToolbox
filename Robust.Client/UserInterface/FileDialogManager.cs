using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "CommentTypo")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal sealed class FileDialogManager : IFileDialogManagerInternal
    {
        // Uses nativefiledialog to open the file dialogs cross platform.
        // On Linux, if the kdialog command is found, it will be used instead.
        // TODO: Should we maybe try to avoid running kdialog if the DE isn't KDE?

#if LINUX
        private bool _kDialogAvailable;
#endif

        public Task<string> OpenFile()
        {
#if LINUX
            if (_kDialogAvailable)
            {
                return OpenFileKDialog();
            }
#endif
            return OpenFileNfd();
        }

        public Task<string> SaveFile()
        {
#if LINUX
            if (_kDialogAvailable)
            {
                return SaveFileKDialog();
            }
#endif
            return SaveFileNfd();
        }

        public Task<string> OpenFolder()
        {
#if LINUX
            if (_kDialogAvailable)
            {
                return OpenFolderKDialog();
            }
#endif
            return OpenFolderNfd();
        }

        public void Initialize()
        {
#if LINUX
            CheckKDialogSupport();
#endif
        }

        private static unsafe Task<string> OpenFileNfd()
        {
            // Have to run it in the thread pool to avoid blocking the main thread.
            return Task.Run(() =>
            {
                byte* outPath;

                var result = sw_NFD_OpenDialog(null, null, &outPath);

                return HandleNfdResult(result, outPath);
            });
        }

        private static unsafe Task<string> SaveFileNfd()
        {
            // Have to run it in the thread pool to avoid blocking the main thread.
            return Task.Run(() =>
            {
                byte* outPath;

                var result = sw_NFD_SaveDialog(null, null, &outPath);

                return HandleNfdResult(result, outPath);
            });
        }

        private static unsafe Task<string> OpenFolderNfd()
        {
            // Have to run it in the thread pool to avoid blocking the main thread.
            return Task.Run(() =>
            {
                byte* outPath;

                var result = sw_NFD_PickFolder(null, &outPath);

                return HandleNfdResult(result, outPath);
            });
        }

        private static unsafe string HandleNfdResult(sw_nfdresult result, byte* outPath)
        {
            switch (result)
            {
                case sw_nfdresult.SW_NFD_ERROR:
                    var errPtr = sw_NFD_GetError();
                    throw new Exception(MarshalHelper.PtrToStringUTF8(errPtr));

                case sw_nfdresult.SW_NFD_OKAY:
                    var str = MarshalHelper.PtrToStringUTF8(outPath);

                    sw_NFD_Free(outPath);
                    return str;

                case sw_nfdresult.SW_NFD_CANCEL:
                    return null;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#if LINUX
        private void CheckKDialogSupport()
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

            process.WaitForExit();
            _kDialogAvailable = process.ExitCode == 0;

            if (_kDialogAvailable)
            {
                Logger.DebugS("filedialog", "kdialog available.");
            }
        }

        private static Task<string> OpenFileKDialog()
        {
            return RunKDialog("--getopenfilename");
        }

        private static Task<string> SaveFileKDialog()
        {
            return RunKDialog("--getsavefilename");
        }

        private static Task<string> OpenFolderKDialog()
        {
            return RunKDialog("--getexistingdirectory");
        }

        private static async Task<string> RunKDialog(string option)
        {
            var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "kdialog",
                    Arguments = option,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    StandardOutputEncoding = EncodingHelpers.UTF8
                });

            DebugTools.AssertNotNull(process);

            await process.WaitForExitAsync();

            // Cancel hit.
            if (process.ExitCode == 1)
            {
                return null;
            }

            return (await process.StandardOutput.ReadLineAsync()).Trim();
        }
#endif

        [DllImport("swnfd.dll")]
        private static extern unsafe byte* sw_NFD_GetError();

        [DllImport("swnfd.dll")]
        private static extern unsafe sw_nfdresult
            sw_NFD_OpenDialog(byte* filterList, byte* defaultPath, byte** outPath);

        [DllImport("swnfd.dll")]
        private static extern unsafe sw_nfdresult
            sw_NFD_SaveDialog(byte* filterList, byte* defaultPath, byte** outPath);

        [DllImport("swnfd.dll")]
        private static extern unsafe sw_nfdresult
            sw_NFD_PickFolder(byte* defaultPath, byte** outPath);

        [DllImport("swnfd.dll")]
        private static extern unsafe void sw_NFD_Free(void* ptr);

        private enum sw_nfdresult
        {
            SW_NFD_ERROR,
            SW_NFD_OKAY,
            SW_NFD_CANCEL,
        }
    }

    [UsedImplicitly]
    internal sealed class TestOpenFileCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "testopenfile";
        public string Description => string.Empty;
        public string Help => string.Empty;

        public bool Execute(IDebugConsole console, params string[] args)
        {
            Inner(console);
            return false;
        }

        private static async void Inner(IDebugConsole console)
        {
            var manager = IoCManager.Resolve<IFileDialogManager>();
            var path = await manager.OpenFile();

            console.AddLine(path ?? string.Empty);
        }
    }
}
