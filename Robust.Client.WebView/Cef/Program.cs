using System;
using System.IO;
using System.Runtime.InteropServices;
using Robust.Shared.ContentPack;
using Xilium.CefGlue;

namespace Robust.Client.WebView.Cef
{
    public static class Program
    {
        // This was supposed to be the main entry for the subprocess program... It doesn't work.
        public static int Main(string[] args)
        {
            // This is a workaround for this to work on UNIX.
            var argv = args;
            if (CefRuntime.Platform != CefRuntimePlatform.Windows)
            {
                argv = new string[args.Length + 1];
                Array.Copy(args, 0, argv, 1, args.Length);
                argv[0] = "-";
            }
/* 
            if (OperatingSystem.IsLinux())
            {
                // Chromium tries to load libEGL.so and libGLESv2.so relative to the process executable on Linux.
                // (Compared to Windows where it is relative to Chromium's *module*)
                // There is a TODO "is this correct?" in the Chromium code for this.
                // Great.
                
                //CopyDllToExecutableDir("libEGL.so");
                //CopyDllToExecutableDir("libGLESv2.so");

                // System.Threading.Thread.Sleep(200000);
            }
 */
            var mainArgs = new CefMainArgs(argv);

            // This will block executing until the subprocess is shut down.
            var code = CefRuntime.ExecuteProcess(mainArgs, null, IntPtr.Zero);

            if (code != 0)
            {
                System.Console.WriteLine($"CEF Subprocess exited unsuccessfully with exit code {code}! Arguments: {string.Join(' ', argv)}");
            }

            return code;
        }

/*         private static void CopyDllToExecutableDir(string dllName)
        {
            var executableDir = PathHelpers.GetExecutableDirectory();
            var targetPath = Path.Combine(executableDir, dllName);
            if (File.Exists(targetPath))
                return;

            // Find source file.
            string? srcFile = null;
            foreach (var searchDir in WebViewManagerCef.NativeDllSearchDirectories())
            {
                var searchPath = Path.Combine(searchDir, dllName);
                if (File.Exists(searchPath))
                {
                    srcFile = searchPath;
                    break;
                }
            }

            if (srcFile == null)
                return;

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (File.Exists(targetPath))
                        return;

                    File.Copy(srcFile, targetPath);
                    return;
                }
                catch 
                {
                    // Catching race condition lock errors and stuff I guess.
                }
            }
        } */
    }
}
