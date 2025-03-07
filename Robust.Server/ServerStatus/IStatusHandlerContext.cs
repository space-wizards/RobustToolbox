using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// Stream that reads the request body data,
        /// </summary>
        Stream RequestBody { get; }
        Uri Url { get; }
        bool IsGetLike { get; }
        IReadOnlyDictionary<string, StringValues> RequestHeaders { get; }

        IDictionary<string, string> ResponseHeaders { get; }
        bool KeepAlive { get; set; }

        Task<T?> RequestBodyJsonAsync<T>();

        Task RespondNoContentAsync();

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

        Task RespondErrorAsync(HttpStatusCode code);

        Task RespondJsonAsync(object jsonData, HttpStatusCode code = HttpStatusCode.OK);

        Task<Stream> RespondStreamAsync(HttpStatusCode code = HttpStatusCode.OK);
    }
}
