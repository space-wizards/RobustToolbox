using System.Net.Http;
using System.Net.Http.Headers;

namespace Robust.Shared.Utility;

internal static class HttpClientUserAgent
{
    private const string ProductName = "RobustToolbox";

    /// <summary>
    /// Add a Robust-specific user agent to the default request headers of the given <see cref="HttpClient"/>.
    /// </summary>
    public static void AddUserAgent(HttpClient client)
    {
        var assemblyName = typeof(HttpClientUserAgent).Assembly.GetName();
        if (assemblyName.Version is { } version)
        {
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(ProductName, version.ToString()));
        }
    }
}
