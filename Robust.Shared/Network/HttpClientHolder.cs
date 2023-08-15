using System.Net.Http;
using Robust.Shared.Utility;

namespace Robust.Shared.Network;

/// <summary>
/// Holds a shared <see cref="HttpClient"/> for the whole program to use.
/// </summary>
/// <remarks>
/// <para>
/// The shared <see cref="HttpClient"/> has an appropriate <c>User-Agent</c> set for Robust,
/// and correctly supports Happy Eyeballs.
/// </para>
/// <para>
/// This interface is not available on the client.
/// Engine code may use <see cref="HttpClientHolder"/> directly instead,
/// content code can't send arbitrary HTTP requests.
/// </para>
/// </remarks>
public interface IHttpClientHolder
{
    HttpClient Client { get; }
}

/// <summary>
/// Implementation of <see cref="IHttpClientHolder"/>.
/// </summary>
internal sealed class HttpClientHolder : IHttpClientHolder
{
    public HttpClient Client { get; }

    public HttpClientHolder()
    {
        Client = new HttpClient(HappyEyeballsHttp.CreateHttpHandler());
        HttpClientUserAgent.AddUserAgent(Client);
    }
}


