using System.Diagnostics.CodeAnalysis;
using Robust.Client.WebView;
using Robust.Client.WebView.Cef;
using Robust.Client.WebView.Headless;
using Robust.Client.WebViewHook;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

[assembly: WebViewManagerImpl(typeof(WebViewManager))]

namespace Robust.Client.WebView
{
    internal sealed class WebViewManager : IWebViewManagerInternal, IWebViewManagerHook
    {
        private IWebViewManagerImpl? _impl;

        public void PreInitialize(IDependencyCollection dependencies, GameController.DisplayMode mode)
        {
            DebugTools.Assert(_impl == null, "WebViewManager has already been initialized!");

            var cfg = dependencies.Resolve<IConfigurationManagerInternal>();
            cfg.LoadCVarsFromAssembly(typeof(WebViewManager).Assembly);

            var refl = dependencies.Resolve<IReflectionManager>();
            refl.LoadAssemblies(typeof(WebViewManager).Assembly);

            dependencies.RegisterInstance<IWebViewManager>(this);
            dependencies.RegisterInstance<IWebViewManagerInternal>(this);

            if (mode == GameController.DisplayMode.Headless || cfg.GetCVar(WCVars.WebHeadless))
                _impl = new WebViewManagerHeadless();
            else
                _impl = new WebViewManagerCef();

            dependencies.InjectDependencies(_impl, oneOff: true);
        }

        public void Initialize()
        {
            DebugTools.Assert(_impl != null, "WebViewManager has not yet been initialized!");

            _impl!.Initialize();
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

        public void SetResourceMimeType(string extension, string mimeType)
        {
            DebugTools.Assert(_impl != null, "WebViewManager has not yet been initialized!");

            _impl!.SetResourceMimeType(extension, mimeType);
        }

        public bool TryGetResourceMimeType(string extension, [NotNullWhen(true)] out string? mimeType)
        {
            DebugTools.Assert(_impl != null, "WebViewManager has not yet been initialized!");

            return _impl!.TryGetResourceMimeType(extension, out mimeType);
        }

        public IWebViewControlImpl MakeControlImpl(WebViewControl owner)
        {
            DebugTools.Assert(_impl != null, "WebViewManager has not yet been initialized!");

            return _impl!.MakeControlImpl(owner);
        }
    }
}
