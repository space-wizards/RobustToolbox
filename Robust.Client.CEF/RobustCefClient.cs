using Xilium.CefGlue;

namespace Robust.Client.CEF
{
    // Simple CEF client.
    internal class RobustCefClient : CefClient
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
