using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Robust.Shared.Utility;

namespace Robust.Server.ServerStatus
{
    public static class StatusHostHelpers
    {
        public static bool IsGetLike(this HttpMethod method)
        {
            return method == HttpMethod.Get || method == HttpMethod.Head;
        }

        public static void Respond(
            this HttpListenerResponse response,
            HttpMethod method,
            string text,
            HttpStatusCode code = HttpStatusCode.OK,
            string contentType = "text/plain")
        {
            response.Respond(method, text, (int) code, contentType);
        }

        public static void Respond(
            this HttpListenerResponse response,
            HttpMethod method,
            string text,
            int code = 200,
            string contentType = "text/plain")
        {
            response.StatusCode = code;
            response.ContentType = contentType;

            if (method == HttpMethod.Head)
            {
                return;
            }

            using var writer = new StreamWriter(response.OutputStream, EncodingHelpers.UTF8);

            writer.Write(text);
        }

        [return: MaybeNull]
        public static T GetFromJson<T>(this HttpListenerRequest request)
        {
            using var streamReader = new StreamReader(request.InputStream, EncodingHelpers.UTF8);
            using var jsonReader = new JsonTextReader(streamReader);

            var serializer = new JsonSerializer();
            return serializer.Deserialize<T>(jsonReader);
        }
    }
}
