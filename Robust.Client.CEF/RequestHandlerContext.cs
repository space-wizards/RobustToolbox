using System;
using System.IO;
using System.Net;
using Xilium.CefGlue;

namespace Robust.Client.CEF
{
    public sealed class RequestHandlerContext
    {
        internal readonly CefRequest CefRequest;

        public bool IsNavigation { get; }
        public bool IsDownload { get; }
        public string RequestInitiator { get; }

        public string Url => CefRequest.Url;
        public string Method => CefRequest.Method;

        public bool IsHandled { get; private set; }

        public bool IsCancelled { get; private set; }

        internal IRequestResult? Result { get; private set; }

        internal RequestHandlerContext(
            bool isNavigation,
            bool isDownload,
            string requestInitiator,
            CefRequest cefRequest)
        {
            CefRequest = cefRequest;
            IsNavigation = isNavigation;
            IsDownload = isDownload;
            RequestInitiator = requestInitiator;
        }

        public void DoCancel()
        {
            CheckNotHandled();

            IsHandled = true;
            IsCancelled = true;
        }

        public void DoRespondStream(Stream stream, string contentType, HttpStatusCode code = HttpStatusCode.OK)
        {
            Result = new RequestResultStream(stream, contentType, code);
        }

        private void CheckNotHandled()
        {
            if (IsHandled)
                throw new InvalidOperationException("Request has already been handled");
        }
    }
}
