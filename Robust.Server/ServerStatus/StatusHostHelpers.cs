using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Robust.Shared.Utility;

namespace Robust.Server.ServerStatus
{
    public static class StatusHostHelpers
    {
        public static bool IsGetLike(this HttpMethod method) =>
            method == HttpMethod.Get || method == HttpMethod.Head;

        public static void Respond(this HttpResponse response, string text, HttpStatusCode code = HttpStatusCode.OK,
            string contentType = "text/plain") =>
            response.Respond(text, (int) code, contentType);

        public static void Respond(this HttpResponse response, string text, int code = 200,
            string contentType = "text/plain")
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

        [return: MaybeNull]
        public static T GetFromJson<T>(this HttpRequest request)
        {
            using var streamReader = new StreamReader(request.Body, EncodingHelpers.UTF8);
            using var jsonReader = new JsonTextReader(streamReader);

            var serializer = new JsonSerializer();
            return serializer.Deserialize<T>(jsonReader);
        }
    }
}
