using System.IO;
using System.Net;
using System.Net.Http;
using Robust.Shared.Utility;
using Mono.Net;
using HttpListenerResponse = Mono.Net.HttpListenerResponse;

namespace Robust.Server.ServerStatus
{
    public static class StatusHostHelpers
    {
        public static bool IsGetLike(this HttpMethod method)
        {
            return method == HttpMethod.Get || method == HttpMethod.Head;
        }

        public static void Respond(this HttpListenerResponse response, HttpMethod method, string text, HttpStatusCode code,
            string contentType)
        {
            response.StatusCode = (int) code;
            response.ContentEncoding = EncodingHelpers.UTF8;
            response.ContentType = contentType;

            if (method != HttpMethod.Head)
            {
                using (var writer = new StreamWriter(response.OutputStream, EncodingHelpers.UTF8))
                {
                    writer.Write(text);
                }
            }

            response.Close();
        }
    }
}
