using System;
using Xilium.CefGlue;

namespace Robust.Client.WebView
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

            // This will block executing until the subprocess is shut down.
            var code = CefRuntime.ExecuteProcess(mainArgs, new RobustCefApp(), IntPtr.Zero);

            if (code != 0)
            {
                System.Console.WriteLine($"CEF Subprocess exited unsuccessfully with exit code {code}! Arguments: {string.Join(' ', argv)}");
            }

            return code;
        }
    }
}
