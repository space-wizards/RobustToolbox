using System;
using Robust.Shared.Log;
using Xilium.CefGlue;

namespace Robust.Client.CEF
{
    public class RobustCefApp : CefApp
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
            // Disable zygote. TODO CEF: Do research on this?
            //commandLine.AppendSwitch("--no-zygote");
            //commandLine.AppendSwitch("--zygote");

            // We use single-process for now as multi-process requires us to ship a native program
            //commandLine.AppendSwitch("--single-process");

            // We do CPU rendering, disable the GPU...
            //commandLine.AppendSwitch("--disable-gpu");
            //commandLine.AppendSwitch("--disable-gpu-compositing");
            commandLine.AppendSwitch("--in-process-gpu");

            commandLine.AppendSwitch("disable-threaded-scrolling", "1");
            commandLine.AppendSwitch("disable-features", "TouchpadAndWheelScrollLatching,AsyncWheelEvents");

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
