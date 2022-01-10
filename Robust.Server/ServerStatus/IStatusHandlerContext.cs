using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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

        [Obsolete("Use async versions instead")]
        T? RequestBodyJson<T>();
        Task<T?> RequestBodyJsonAsync<T>();

        [Obsolete("Use async versions instead")]
        void Respond(
            string text,
            HttpStatusCode code = HttpStatusCode.OK,
            string contentType = "text/plain");

        [Obsolete("Use async versions instead")]
        void Respond(
            string text,
            int code = 200,
            string contentType = "text/plain");

        [Obsolete("Use async versions instead")]
        void Respond(
            byte[] data,
            HttpStatusCode code = HttpStatusCode.OK,
            string contentType = "text/plain");

        [Obsolete("Use async versions instead")]
        void Respond(
            byte[] data,
            int code = 200,
            string contentType = "text/plain");

        Task RespondAsync(
            string text,
            HttpStatusCode code = HttpStatusCode.OK,
            string contentType = "text/plain");

        Task RespondAsync(
            string text,
            int code = 200,
            string contentType = "text/plain");

        Task RespondAsync(
            byte[] data,
            HttpStatusCode code = HttpStatusCode.OK,
            string contentType = "text/plain");

        Task RespondAsync(
            byte[] data,
            int code = 200,
            string contentType = "text/plain");

        [Obsolete("Use async versions instead")]
        void RespondError(HttpStatusCode code);

        Task RespondErrorAsync(HttpStatusCode code);

        [Obsolete("Use async versions instead")]
        void RespondJson(object jsonData, HttpStatusCode code = HttpStatusCode.OK);

        Task RespondJsonAsync(object jsonData, HttpStatusCode code = HttpStatusCode.OK);
    }
}
