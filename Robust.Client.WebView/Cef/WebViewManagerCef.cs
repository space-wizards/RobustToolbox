using System;
using System.IO;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Xilium.CefGlue;

namespace Robust.Client.WebView.Cef
{
    internal partial class WebViewManagerCef : IWebViewManagerImpl
    {
        private CefApp _app = default!;

        [Dependency] private readonly IDependencyCollection _dependencyCollection = default!;

        public void Initialize()
        {
            IoCManager.Instance!.InjectDependencies(this, oneOff: true);

            string subProcessName;
            if (OperatingSystem.IsWindows())
                subProcessName = "Robust.Client.WebView.exe";
            else if (OperatingSystem.IsLinux())
                subProcessName = "Robust.Client.WebView";
            else
                throw new NotSupportedException("Unsupported platform for CEF!");

            var subProcessPath = PathHelpers.ExecutableRelativeFile(subProcessName);

            var settings = new CefSettings()
            {
                WindowlessRenderingEnabled = true, // So we can render to our UI controls.
                ExternalMessagePump = false, // Unsure, honestly. TODO CEF: Research this?
                NoSandbox = true, // Not disabling the sandbox crashes CEF.
                BrowserSubprocessPath = subProcessPath,
                LocalesDirPath = Path.Combine(PathHelpers.GetExecutableDirectory(), "locales"),
                ResourcesDirPath = PathHelpers.GetExecutableDirectory(),
                RemoteDebuggingPort = 9222
            };

            Logger.Info($"CEF Version: {CefRuntime.ChromeVersion}");

            // --------------------------- README --------------------------------------------------
            // By the way! You're gonna need the CEF binaries in your client's bin folder.
            // More specifically, version cef_binary_94.4.1+g4b61a8c+chromium-94.0.4606.54
            // Windows: https://cef-builds.spotifycdn.com/cef_binary_94.4.1%2Bg4b61a8c%2Bchromium-94.0.4606.54_windows64_minimal.tar.bz2
            // Linux: https://cef-builds.spotifycdn.com/cef_binary_94.4.1%2Bg4b61a8c%2Bchromium-94.0.4606.54_linux64_minimal.tar.bz2
            // Here's how to get it to work:
            // 1. Copy all the contents of "Release" to the bin/Content.Client folder.
            // 2. Copy all the contents of "Resources" to the bin/Content.Client folder.
            // -------------------------------------------------------------------------------------

            _app = new RobustCefApp();

            // We pass no main arguments...
            CefRuntime.Initialize(new CefMainArgs(null), settings, _app, IntPtr.Zero);

            // TODO CEF: After this point, debugging breaks. No, literally. My client crashes but ONLY with the debugger.
            // I have tried using the DEBUG and RELEASE versions of libcef.so, stripped or non-stripped...
            // And nothing seemed to work. Odd.
        }

        public void Update()
        {
            // Calling this makes CEF do its work, without using its own update loop.
            CefRuntime.DoMessageLoopWork();
        }

        public void Shutdown()
        {
            CefRuntime.Shutdown();
        }
    }
}
