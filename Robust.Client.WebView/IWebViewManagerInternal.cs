namespace Robust.Client.WebView
{
    internal interface IWebViewManagerInternal : IWebViewManager
    {
        IWebViewControlImpl MakeControlImpl(WebViewControl owner);
    }
}
