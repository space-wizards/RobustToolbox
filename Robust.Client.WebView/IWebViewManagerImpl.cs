using Robust.Client.WebViewHook;

namespace Robust.Client.WebView
{
    /// <summary>
    /// Internal implementation of WebViewManager that is switched out by <see cref="IWebViewManagerHook"/>.
    /// </summary>
    internal interface IWebViewManagerImpl : IWebViewManagerInternal
    {
        void Initialize();
        void Update();
        void Shutdown();
    }
}
