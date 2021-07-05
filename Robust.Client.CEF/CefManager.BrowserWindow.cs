using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
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
            internal readonly CefBrowser Browser;
            private readonly CefManager _manager;

            [ViewVariables(VVAccess.ReadWrite)]
            public string Url
            {
                get => Browser.GetMainFrame().Url;
                set => Browser.GetMainFrame().LoadUrl(value);
            }

            [ViewVariables]
            public bool IsLoading => Browser.IsLoading;


            public BrowserWindowImpl(CefManager manager, CefBrowser browser)
            {
                Browser = browser;
                _manager = manager;
            }

            public void StopLoad()
            {
                Browser.StopLoad();
            }

            public void Reload()
            {
                Browser.Reload();
            }

            public bool GoBack()
            {
                if (!Browser.CanGoBack)
                    return false;

                Browser.GoBack();
                return true;
            }

            public bool GoForward()
            {
                if (!Browser.CanGoForward)
                    return false;

                Browser.GoForward();
                return true;
            }

            public void ExecuteJavaScript(string code)
            {
                Browser.GetMainFrame().ExecuteJavaScript(code, string.Empty, 1);
            }
        }

        private sealed class WindowWebClient : CefClient
        {
        }
    }
}
