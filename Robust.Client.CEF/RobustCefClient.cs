using Xilium.CefGlue;

namespace Robust.Client.CEF
{
    // Simple CEF client.
    internal class RobustCefClient : CefClient
    {
        private readonly CefRenderHandler _renderHandler;
        private readonly CefRequestHandler _requestHandler;

        internal RobustCefClient(CefRenderHandler handler, CefRequestHandler requestHandler)
        {
            _renderHandler = handler;
            _requestHandler = requestHandler;
        }

        protected override CefRenderHandler GetRenderHandler() => _renderHandler;
        protected override CefRequestHandler GetRequestHandler() => _requestHandler;
    }
}
