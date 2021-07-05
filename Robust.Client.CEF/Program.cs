using System;
using Xilium.CefGlue;

namespace Robust.Client.CEF
{
    public static class Program
    {
        // This was supposed to be the main entry for the subprocess program... It doesn't work.
        public static int Main(string[] args)
        {
            var mainArgs = new CefMainArgs(args);
            // var app = new RobustCefApp();

            // This will block executing IF this is a proper subprocess but it was broken and it returned -1
            // -1 means this process is the main method which... Wasn't possible.
            // We probably need a native program?
            var code = CefRuntime.ExecuteProcess(mainArgs, null, IntPtr.Zero);

            if (code != 0)
            {
                System.Console.WriteLine($"CEF Subprocess exited with exit code {code}! Arguments: {string.Join(' ', args)}");
            }

            return code;
        }
    }
}
