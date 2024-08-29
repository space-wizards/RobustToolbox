using Xilium.CefGlue;

namespace Robust.Client.WebView.Cef
{
    // Simple CEF client.
    internal sealed class RobustCefClient : BaseRobustCefClient
    {
        private readonly CefRenderHandler _renderHandler;
        private readonly CefRequestHandler _requestHandler;
        private readonly CefLoadHandler _loadHandler;

        internal RobustCefClient(CefRenderHandler handler, CefRequestHandler requestHandler, CefLoadHandler loadHandler)
        {
            _renderHandler = handler;
            _requestHandler = requestHandler;
            _loadHandler = loadHandler;
        }

        protected override CefRenderHandler GetRenderHandler() => _renderHandler;
        protected override CefRequestHandler GetRequestHandler() => _requestHandler;
        protected override CefLoadHandler GetLoadHandler() => _loadHandler;
    }
}
