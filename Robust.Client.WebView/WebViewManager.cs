using Robust.Client.WebView;
using Robust.Client.WebView.Cef;
using Robust.Client.WebViewHook;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

[assembly: WebViewManagerImpl(typeof(WebViewManager))]

namespace Robust.Client.WebView
{
    internal sealed class WebViewManager : IWebViewManagerInternal, IWebViewManagerHook
    {
        private IWebViewManagerImpl? _impl;

        public void Initialize()
        {
            DebugTools.Assert(_impl == null, "WebViewManager has already been initialized!");

            IoCManager.RegisterInstance<IWebViewManager>(this);
            IoCManager.RegisterInstance<IWebViewManagerInternal>(this);

            _impl = new WebViewManagerCef();
            _impl.Initialize();
        }

        public void Update()
        {
            DebugTools.Assert(_impl != null, "WebViewManager has not yet been initialized!");

            _impl!.Update();
        }

        public void Shutdown()
        {
            DebugTools.Assert(_impl != null, "WebViewManager has not yet been initialized!");

            _impl!.Shutdown();
        }

        public IWebViewWindow CreateBrowserWindow(BrowserWindowCreateParameters createParams)
        {
            DebugTools.Assert(_impl != null, "WebViewManager has not yet been initialized!");

            return _impl!.CreateBrowserWindow(createParams);
        }

        public IWebViewControlImpl MakeControlImpl(WebViewControl owner)
        {
            DebugTools.Assert(_impl != null, "WebViewManager has not yet been initialized!");

            return _impl!.MakeControlImpl(owner);
        }
    }
}
