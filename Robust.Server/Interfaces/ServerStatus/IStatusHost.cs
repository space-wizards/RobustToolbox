using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace Robust.Server.Interfaces.ServerStatus
{
    public delegate bool StatusHostHandler(
        IStatusHandlerContext context);

    public interface IStatusHost
    {
        void Start();

        void AddHandler(StatusHostHandler handler);

        /// <summary>
        ///     Invoked when a client queries a status request from the server.
        ///     THIS IS INVOKED FROM ANOTHER THREAD.
        ///     I REPEAT, THIS DOES NOT RUN ON THE MAIN THREAD.
        ///     MAKE TRIPLE SURE EVERYTHING IN HERE IS THREAD SAFE DEAR GOD.
        /// </summary>
        event Action<JObject> OnStatusRequest;

        /// <summary>
        ///     Invoked when a client queries an info request from the server.
        ///     THIS IS INVOKED FROM ANOTHER THREAD.
        ///     I REPEAT, THIS DOES NOT RUN ON THE MAIN THREAD.
        ///     MAKE TRIPLE SURE EVERYTHING IN HERE IS THREAD SAFE DEAR GOD.
        /// </summary>
        event Action<JObject> OnInfoRequest;
    }

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
