using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Xilium.CefGlue;

namespace Robust.Client.CEF
{
    public partial class CefManager
    {
        [Dependency] private readonly IClydeInternal _clyde = default!;

        private readonly List<BrowserWindowImpl> _browserWindows = new();

        public IBrowserWindow CreateBrowserWindow(BrowserWindowCreateParameters createParams)
        {
            var mainHWnd = (_clyde.MainWindow as IClydeWindowInternal)?.WindowsHWnd ?? 0;

            var info = CefWindowInfo.Create();
            info.Width = createParams.Width;
            info.Height = createParams.Height;
            info.SetAsPopup(mainHWnd, "ss14cef");

            var settings = new CefBrowserSettings();

            var browser = CefBrowserHost.CreateBrowserSync(info, new WindowWebClient(), settings, createParams.Url);

            var impl = new BrowserWindowImpl(this, browser);

            _browserWindows.Add(impl);

            return impl;
        }

        private sealed class BrowserWindowImpl : IBrowserWindow
        {
            public readonly CefBrowser Browser;
            private readonly CefManager _manager;

            public BrowserWindowImpl(CefManager manager, CefBrowser browser)
            {
                Browser = browser;
                _manager = manager;
            }
        }

        private sealed class WindowWebClient : CefClient
        {
        }
    }
}
