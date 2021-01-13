using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Robust.Shared.Utility;

namespace Robust.Server.ServerStatus
{
    public static class StatusHostHelpers
    {
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
