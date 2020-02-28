using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Robust.Shared.Utility;

namespace Robust.Server.ServerStatus
{

    public static class StatusHostHelpers
    {

        public static bool IsGetLike(this HttpMethod method) =>
            method == HttpMethod.Get || method == HttpMethod.Head;

        public static void Respond(this HttpResponse response, string text, HttpStatusCode code = HttpStatusCode.OK, string contentType = "text/plain") =>
            response.Respond(text, (int) code, contentType);

        public static void Respond(this HttpResponse response, string text, int code = 200, string contentType = "text/plain")
        {
            response.StatusCode = code;
            response.ContentType = contentType;

            if (response.HttpContext.Request.Method == "HEAD")
            {
                return;
            }

            using var writer = new StreamWriter(response.Body, EncodingHelpers.UTF8);

            writer.Write(text);
        }

    }

}
