namespace Robust.Client.WebView
{
    public interface IWebViewManager
    {
        IWebViewWindow CreateBrowserWindow(BrowserWindowCreateParameters createParams);
    }
}
