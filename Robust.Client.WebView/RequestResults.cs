using System;
using System.IO;
using System.Net;
using Xilium.CefGlue;

namespace Robust.Client.WebView
{
    internal interface IRequestResult
    {
        CefResourceHandler MakeHandler();
    }

    internal sealed class RequestResultStream : IRequestResult
    {
        private readonly Stream _stream;
        private readonly HttpStatusCode _code;
        private readonly string _contentType;

        public RequestResultStream(Stream stream, string contentType, HttpStatusCode code)
        {
            _stream = stream;
            _code = code;
            _contentType = contentType;
        }

        public CefResourceHandler MakeHandler()
        {
            return new Handler(_stream, _contentType, _code);
        }

        private sealed class Handler : CefResourceHandler
        {
            // TODO: async
            // TODO: exception handling

            private readonly Stream _stream;
            private readonly HttpStatusCode _code;
            private readonly string _contentType;

            public Handler(Stream stream, string contentType, HttpStatusCode code)
            {
                _stream = stream;
                _code = code;
                _contentType = contentType;
            }

            protected override bool Open(CefRequest request, out bool handleRequest, CefCallback callback)
            {
                handleRequest = true;
                return true;
            }

            protected override void GetResponseHeaders(CefResponse response, out long responseLength, out string? redirectUrl)
            {
                response.Status = (int) _code;
                response.StatusText = _code.ToString();
                response.MimeType = _contentType;

                if (_stream.CanSeek)
                    responseLength = _stream.Length;
                else
                    responseLength = -1;

                redirectUrl = default;
            }

            protected override bool Skip(long bytesToSkip, out long bytesSkipped, CefResourceSkipCallback callback)
            {
                if (!_stream.CanSeek)
                {
                    bytesSkipped = -2;
                    return false;
                }

                bytesSkipped = _stream.Seek(bytesToSkip, SeekOrigin.Begin);
                return true;
            }

            protected override unsafe bool Read(IntPtr dataOut, int bytesToRead, out int bytesRead, CefResourceReadCallback callback)
            {
                var byteSpan = new Span<byte>((void*) dataOut, bytesToRead);

                bytesRead = _stream.Read(byteSpan);

                return bytesRead != 0;
            }

            protected override void Cancel()
            {
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                _stream.Dispose();
            }
        }
    }
}
