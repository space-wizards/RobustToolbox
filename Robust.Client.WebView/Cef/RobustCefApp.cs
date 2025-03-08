using System;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Xilium.CefGlue;

namespace Robust.Client.WebView.Cef
{
    internal sealed class RobustCefApp : CefApp
    {
        private readonly ISawmill? _sawmill;
        private readonly BrowserProcessHandler _browserProcessHandler = new();
        private readonly RenderProcessHandler _renderProcessHandler = new();

        public RobustCefApp(ISawmill? sawmill)
        {
            _sawmill = sawmill;
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
            // Disable zygote on Linux.
            commandLine.AppendSwitch("--no-zygote");

            // Work around https://github.com/chromiumembedded/cef/issues/3213
            // Desktop GL force makes Chromium not try to load its own ANGLE/Swiftshader so load paths aren't problematic.
            // UPDATE: That bug got fixed and now this workaround breaks CEF.
            // Keeping all this comment history in case I ever wanan remember what the `--use-gl` flag is.
            //if (OperatingSystem.IsLinux())
            //    commandLine.AppendSwitch("--use-gl", "desktop");

            // commandLine.AppendSwitch("--single-process");

            //commandLine.AppendSwitch("--disable-gpu");
            //commandLine.AppendSwitch("--disable-gpu-compositing");
            //commandLine.AppendSwitch("--in-process-gpu");

            commandLine.AppendSwitch("--off-screen-rendering-enabled");

            commandLine.AppendSwitch("disable-threaded-scrolling", "1");
            commandLine.AppendSwitch("disable-features", "TouchpadAndWheelScrollLatching,AsyncWheelEvents");

            _sawmill?.Debug($"CEF command line: {commandLine}");
        }

        protected override void OnRegisterCustomSchemes(CefSchemeRegistrar registrar)
        {
            registrar.AddCustomScheme("res", CefSchemeOptions.Secure | CefSchemeOptions.Standard);
            registrar.AddCustomScheme("usr", CefSchemeOptions.Secure | CefSchemeOptions.Standard);
        }

        private sealed class BrowserProcessHandler : CefBrowserProcessHandler
        {
        }

        // TODO CEF: Research - Is this even needed?
        private sealed class RenderProcessHandler : CefRenderProcessHandler
        {
        }
    }
}
