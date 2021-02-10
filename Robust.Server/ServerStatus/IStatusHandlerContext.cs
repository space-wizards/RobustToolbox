using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Primitives;

namespace Robust.Server.ServerStatus
{
    public interface IStatusHandlerContext
    {
        HttpMethod RequestMethod { get; }
        IPEndPoint RemoteEndPoint { get; }
        Uri Url { get; }
        bool IsGetLike { get; }
        IReadOnlyDictionary<string, StringValues> RequestHeaders { get; }

        [return: MaybeNull]
        public T RequestBodyJson<T>();

        void Respond(
            string text,
            HttpStatusCode code = HttpStatusCode.OK,
            string contentType = "text/plain");

        void Respond(
            string text,
            int code = 200,
            string contentType = "text/plain");

        void RespondError(HttpStatusCode code);

        void RespondJson(object jsonData, HttpStatusCode code = HttpStatusCode.OK);
    }
}
