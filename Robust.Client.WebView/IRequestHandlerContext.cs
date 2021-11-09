using System.IO;
using System.Net;

namespace Robust.Client.WebView
{
    public interface IRequestHandlerContext
    {
        bool IsNavigation { get; }
        bool IsDownload { get; }
        string RequestInitiator { get; }
        string Url { get; }
        string Method { get; }
        bool IsHandled { get; }
        bool IsCancelled { get; }
        void DoCancel();
        void DoRespondStream(Stream stream, string contentType, HttpStatusCode code = HttpStatusCode.OK);
    }
}
