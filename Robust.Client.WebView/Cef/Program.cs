using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
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

            var mainArgs = new CefMainArgs(argv);

            StartWatchThread();

            // This will block executing until the subprocess is shut down.
            var code = CefRuntime.ExecuteProcess(mainArgs, new RobustCefApp(null), IntPtr.Zero);

            if (code != 0)
            {
                System.Console.WriteLine($"CEF Subprocess exited unsuccessfully with exit code {code}! Arguments: {string.Join(' ', argv)}");
            }

            return code;
        }

        private static void StartWatchThread()
        {
            //
            // CEF has this nasty habit of not shutting down all its processes if the parent crashes.
            // Great!
            //
            // We use a separate thread in each CEF child process to watch the main PID.
            // If it exits, we kill ourselves after a couple seconds.
            //

            if (Environment.GetEnvironmentVariable("ROBUST_CEF_BROWSER_PROCESS_ID") is not { } parentIdString)
                return;

            if (Environment.GetEnvironmentVariable("ROBUST_CEF_BROWSER_PROCESS_MODULE") is not { } parentModuleString)
                return;

            if (!int.TryParse(parentIdString, CultureInfo.InvariantCulture, out var parentId))
                return;

            var process = Process.GetProcessById(parentId);
            if ((process.MainModule?.FileName ?? "") != parentModuleString)
            {
                process.Dispose();
                return;
            }

            new Thread(() => WatchThread(process)) { Name = "CEF Watch Thread", IsBackground = true }
                .Start();
        }

        private static void WatchThread(Process p)
        {
            p.WaitForExit();

            Thread.Sleep(3000);

            Environment.Exit(1);
        }
    }
}
