using System;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Xilium.CefGlue;

namespace Robust.Client.WebView.Cef
{
    internal class RobustCefApp : CefApp
    {
        private readonly BrowserProcessHandler _browserProcessHandler = new();
        private readonly RenderProcessHandler _renderProcessHandler = new();

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
            
            // Work around https://bitbucket.org/chromiumembedded/cef/issues/3213/ozone-egl-initialization-does-not-have
            // Desktop GL force makes Chromium not try to load its own ANGLE/Swiftshader so load paths aren't problematic.
            if (OperatingSystem.IsLinux())
                commandLine.AppendSwitch("--use-gl", "desktop");
            
            // commandLine.AppendSwitch("--single-process");

            //commandLine.AppendSwitch("--disable-gpu");
            //commandLine.AppendSwitch("--disable-gpu-compositing");
            //commandLine.AppendSwitch("--in-process-gpu");

            commandLine.AppendSwitch("disable-threaded-scrolling", "1");
            commandLine.AppendSwitch("disable-features", "TouchpadAndWheelScrollLatching,AsyncWheelEvents");

            if(IoCManager.Instance != null)
                Logger.Debug($"{commandLine}");
        }

        private class BrowserProcessHandler : CefBrowserProcessHandler
        {
        }

        // TODO CEF: Research - Is this even needed?
        private class RenderProcessHandler : CefRenderProcessHandler
        {
        }
    }
}
