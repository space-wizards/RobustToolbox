using System;
using System.Collections.Generic;
using Robust.Shared.Log;
using Xilium.CefGlue;

namespace Robust.Client.CEF
{
    internal sealed class RobustRequestHandler : CefRequestHandler
    {
        private readonly ISawmill _sawmill;
        private readonly List<Action<RequestHandlerContext>> _handlers = new();

        public RobustRequestHandler(ISawmill sawmill)
        {
            _sawmill = sawmill;
        }

        public void AddHandler(Action<RequestHandlerContext> handler)
        {
            lock (_handlers)
            {
                _handlers.Add(handler);
            }
        }

        public void RemoveHandler(Action<RequestHandlerContext> handler)
        {
            lock (_handlers)
            {
                _handlers.Remove(handler);
            }
        }

        protected override CefResourceRequestHandler? GetResourceRequestHandler(
            CefBrowser browser,
            CefFrame frame,
            CefRequest request,
            bool isNavigation,
            bool isDownload,
            string requestInitiator,
            ref bool disableDefaultHandling)
        {
            lock (_handlers)
            {
                _sawmill.Debug($"HANDLING REQUEST: {request.Url}");

                var context = new RequestHandlerContext(isNavigation, isDownload, requestInitiator, request);

                foreach (var handler in _handlers)
                {
                    handler(context);

                    if (context.IsHandled)
                        disableDefaultHandling = true;

                    if (context.IsCancelled)
                        return null;

                    if (context.Result != null)
                        return new WrapReaderResourceHandler(context.Result.MakeHandler());
                }
            }

            return null;
        }


        private sealed class WrapReaderResourceHandler : CefResourceRequestHandler
        {
            private readonly CefResourceHandler _handler;

            public WrapReaderResourceHandler(CefResourceHandler handler)
            {
                _handler = handler;
            }

            protected override CefCookieAccessFilter? GetCookieAccessFilter(
                CefBrowser browser,
                CefFrame frame,
                CefRequest request)
            {
                return null;
            }

            protected override CefResourceHandler GetResourceHandler(
                CefBrowser browser,
                CefFrame frame,
                CefRequest request)
            {
                return _handler;
            }
        }
    }
}
