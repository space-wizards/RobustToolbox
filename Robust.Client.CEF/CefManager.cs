using System;
using System.IO;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;

// The library we're using right now. TODO CEF: Do we want to use something else? We will need to ship it ourselves if so.
using Xilium.CefGlue;

namespace Robust.Client.CEF
{
    // Register this with IoC.
    // TODO CEF: think if making this inherit CefApp is a good idea...
    // TODO CEF: A way to handle external window browsers...
    [UsedImplicitly]
    public partial class CefManager : CefApp
    {
        private readonly BrowserProcessHandler _browserProcessHandler;
        private readonly RenderProcessHandler _renderProcessHandler;
        private bool _initialized = false;

        public CefManager()
        {
            // Probably not needed?
            _renderProcessHandler = new RenderProcessHandler();
            _browserProcessHandler = new BrowserProcessHandler();
        }

        /// <summary>
        ///     Call this to initialize CEF.
        /// </summary>
        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            string subProcessName;
            if (OperatingSystem.IsWindows())
                subProcessName = "Robust.Client.CEF.exe";
            else if (OperatingSystem.IsLinux())
                subProcessName = "Robust.Client.CEF";
            else
                throw new NotSupportedException("Unsupported platform for CEF!");

            var subProcessPath = PathHelpers.ExecutableRelativeFile(subProcessName);

            var settings = new CefSettings()
            {
                WindowlessRenderingEnabled = true, // So we can render to our UI controls.
                ExternalMessagePump = false, // Unsure, honestly. TODO CEF: Research this?
                NoSandbox = true, // Not disabling the sandbox crashes CEF.

                // Multi-process currently doesn't work...
                BrowserSubprocessPath = subProcessPath,

                // I don't think this is needed? Research.
                LocalesDirPath = Path.Combine(PathHelpers.GetExecutableDirectory(), "locales"),

                // I don't think this is needed either? Do research.
                ResourcesDirPath = PathHelpers.GetExecutableDirectory(),

            };

            Logger.Info($"CEF Version: {CefRuntime.ChromeVersion}");

            // --------------------------- README --------------------------------------------------
            // By the way! You're gonna need the CEF binaries in your client's bin folder.
            // More specifically, version cef_binary_91.1.21+g9dd45fe+chromium-91.0.4472.114
            // https://cef-builds.spotifycdn.com/cef_binary_91.1.21%2Bg9dd45fe%2Bchromium-91.0.4472.114_windows64_minimal.tar.bz2
            // https://cef-builds.spotifycdn.com/cef_binary_91.1.21%2Bg9dd45fe%2Bchromium-91.0.4472.114_linux64_minimal.tar.bz2
            // Here's how to get it to work:
            // 1. Copy all the contents of "Release" to the bin folder.
            // 2. Copy all the contents of "Resources" to the bin folder.
            // Supposedly, you should just need libcef.so in Release and icudtl.dat in Resources...
            // The rest might be optional.
            // Maybe. Good luck! If you get odd crashes with no info and a weird exit code, use GDB!
            // -------------------------------------------------------------------------------------

            // We pass no main arguments...
            CefRuntime.Initialize(new CefMainArgs(Array.Empty<string>()), settings, this, IntPtr.Zero);

            // TODO CEF: After this point, debugging breaks. No, literally. My client crashes but ONLY with the debugger.
            // I have tried using the DEBUG and RELEASE versions of libcef.so, stripped or non-stripped...
            // And nothing seemed to work. Odd.

            _initialized = true;
        }


        /// <summary>
        ///     Needs to be called regularly for CEF to keep working.
        /// </summary>
        public void Update()
        {
            DebugTools.Assert(_initialized);

            // Calling this makes CEF do its work, without using its own update loop.
            CefRuntime.DoMessageLoopWork();
        }

        /// <summary>
        ///     Call before program shutdown.
        /// </summary>
        public void Shutdown()
        {
            DebugTools.Assert(_initialized);

            Dispose(true);

            CefRuntime.Shutdown();
        }

        protected override CefBrowserProcessHandler GetBrowserProcessHandler()
        {
            return _browserProcessHandler;
        }

        protected override CefRenderProcessHandler GetRenderProcessHandler()
        {
            return _renderProcessHandler;
        }

        protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
        {
            // Disable zygote. TODO CEF: Do research on this?
            commandLine.AppendSwitch("--no-zygote");

            // We use single-process for now as multi-process requires us to ship a native program
            // commandLine.AppendSwitch("--single-process");

            // We do CPU rendering, disable the GPU...
            commandLine.AppendSwitch("--disable-gpu");
            commandLine.AppendSwitch("--disable-gpu-compositing");
            commandLine.AppendSwitch("--in-process-gpu");

            commandLine.AppendSwitch("disable-threaded-scrolling", "1");
            commandLine.AppendSwitch("disable-features", "TouchpadAndWheelScrollLatching,AsyncWheelEvents");

            Logger.Debug($"{commandLine}");
        }

        // TODO CEF: Research - Is this even needed?
        private class BrowserProcessHandler : CefBrowserProcessHandler
        {
        }

        // TODO CEF: Research - Is this even needed?
        private class RenderProcessHandler : CefRenderProcessHandler
        {
        }
    }
}
