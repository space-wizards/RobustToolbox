using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;
using Xilium.CefGlue;

namespace Robust.Client.CEF
{
    public partial class CefManager
    {
        [Dependency] private readonly IClydeInternal _clyde = default!;

        private readonly List<BrowserWindowImpl> _browserWindows = new();

        public IEnumerable<IBrowserWindow> AllBrowserWindows => _browserWindows;

        public IBrowserWindow CreateBrowserWindow(BrowserWindowCreateParameters createParams)
        {
            var mainHWnd = (_clyde.MainWindow as IClydeWindowInternal)?.WindowsHWnd ?? 0;

            var info = CefWindowInfo.Create();
            info.Width = createParams.Width;
            info.Height = createParams.Height;
            info.SetAsPopup(mainHWnd, "ss14cef");

            var impl = new BrowserWindowImpl(this);

            var lifeSpanHandler = new WindowLifeSpanHandler(impl);
            var reqHandler = new RobustRequestHandler(Logger.GetSawmill("root"));
            var client = new WindowCefClient(lifeSpanHandler, reqHandler);
            var settings = new CefBrowserSettings();

            impl.Browser = CefBrowserHost.CreateBrowserSync(info, client, settings, createParams.Url);
            impl.RequestHandler = reqHandler;

            _browserWindows.Add(impl);

            return impl;
        }

        private sealed class BrowserWindowImpl : IBrowserWindow
        {
            private readonly CefManager _manager;
            internal CefBrowser Browser = default!;
            internal RobustRequestHandler RequestHandler = default!;

            public Action<RequestHandlerContext>? OnResourceRequest { get; set; }

            [ViewVariables(VVAccess.ReadWrite)]
            public string Url
            {
                get
                {
                    CheckClosed();
                    return Browser.GetMainFrame().Url;
                }
                set
                {
                    CheckClosed();
                    Browser.GetMainFrame().LoadUrl(value);
                }
            }

            [ViewVariables]
            public bool IsLoading
            {
                get
                {
                    CheckClosed();
                    return Browser.IsLoading;
                }
            }

            public BrowserWindowImpl(CefManager manager)
            {
                _manager = manager;
            }

            public void StopLoad()
            {
                CheckClosed();
                Browser.StopLoad();
            }

            public void Reload()
            {
                CheckClosed();
                Browser.Reload();
            }

            public bool GoBack()
            {
                CheckClosed();
                if (!Browser.CanGoBack)
                    return false;

                Browser.GoBack();
                return true;
            }

            public bool GoForward()
            {
                CheckClosed();
                if (!Browser.CanGoForward)
                    return false;

                Browser.GoForward();
                return true;
            }

            public void ExecuteJavaScript(string code)
            {
                CheckClosed();
                Browser.GetMainFrame().ExecuteJavaScript(code, string.Empty, 1);
            }

            public void AddResourceRequestHandler(Action<RequestHandlerContext> handler)
            {
                RequestHandler.AddHandler(handler);
            }

            public void RemoveResourceRequestHandler(Action<RequestHandlerContext> handler)
            {
                RequestHandler.RemoveHandler(handler);
            }

            public void Dispose()
            {
                if (Closed)
                    return;

                Browser.GetHost().CloseBrowser(true);
                Closed = true;
            }

            public bool Closed { get; private set; }

            public void OnClose()
            {
                Closed = true;
                _manager._browserWindows.Remove(this);
                Logger.Debug("Removing window");
            }

            private void CheckClosed()
            {
                if (Closed)
                    throw new ObjectDisposedException("BrowserWindow");
            }
        }

        private sealed class WindowCefClient : CefClient
        {
            private readonly CefLifeSpanHandler _lifeSpanHandler;
            private readonly CefRequestHandler _requestHandler;

            public WindowCefClient(CefLifeSpanHandler lifeSpanHandler, CefRequestHandler requestHandler)
            {
                _lifeSpanHandler = lifeSpanHandler;
                _requestHandler = requestHandler;
            }

            protected override CefLifeSpanHandler GetLifeSpanHandler() => _lifeSpanHandler;
            protected override CefRequestHandler GetRequestHandler() => _requestHandler;
        }

        private sealed class WindowLifeSpanHandler : CefLifeSpanHandler
        {
            private readonly BrowserWindowImpl _windowImpl;

            public WindowLifeSpanHandler(BrowserWindowImpl windowImpl)
            {
                _windowImpl = windowImpl;
            }

            protected override void OnBeforeClose(CefBrowser browser)
            {
                base.OnBeforeClose(browser);

                _windowImpl.OnClose();
            }
        }
    }
}
