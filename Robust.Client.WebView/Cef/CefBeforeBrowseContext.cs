using Xilium.CefGlue;

namespace Robust.Client.WebView.Cef
{
    internal sealed class CefBeforeBrowseContext : IBeforeBrowseContext
    {
        internal readonly CefRequest CefRequest;

        public string Url => CefRequest.Url;
        public string Method => CefRequest.Method;

        public bool IsRedirect { get; }
        public bool UserGesture { get; }

        public bool IsCancelled { get; private set; }

        internal CefBeforeBrowseContext(
            bool isRedirect,
            bool userGesture,
            CefRequest cefRequest)
        {
            CefRequest = cefRequest;
            IsRedirect = isRedirect;
            UserGesture = userGesture;
        }

        public void DoCancel()
        {
            IsCancelled = true;
        }
    }
}
