using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Robust.Shared.Network;

public sealed class HttpManager : IHttpManagerInternal
{
    private readonly HttpClient _client = new();

    void IHttpManagerInternal.Shutdown()
    {
        _client.CancelPendingRequests();
    }

    public async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancel = default)
    {
        // Uri can be inherited which means it could be inherited by the user
        // Am I going to check if that means they could modify it after the local address check?
        // No, so we copy the original string instead just in case
        // !!FUN!!
        uri = new Uri(uri.OriginalString);
        ThrowIfLocalAddress(uri);
        return await _client.GetStreamAsync(uri, cancel);
    }

    public async Task<string> GetStringAsync(Uri uri, CancellationToken cancel = default)
    {
        uri = new Uri(uri.OriginalString);
        ThrowIfLocalAddress(uri);
        return await _client.GetStringAsync(uri, cancel);
    }

    public async Task<T?> GetFromJsonAsync<T>(Uri uri, CancellationToken cancel = default)
    {
        uri = new Uri(uri.OriginalString);
        ThrowIfLocalAddress(uri);
        return await _client.GetFromJsonAsync<T>(uri, cancel);
    }

    private void ThrowIfLocalAddress(Uri uri)
    {
        if (!IPAddress.TryParse(uri.Host, out var ip))
            return;

        if (IPAddress.IsLoopback(ip) ||
            ip.AddressFamily == AddressFamily.InterNetwork ||
            ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            throw new InvalidAddressException($"{uri} is a local address");
        }
    }
}

public sealed class InvalidAddressException : ArgumentException
{
    internal InvalidAddressException()
    {
    }

    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
    internal InvalidAddressException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    internal InvalidAddressException(string? message) : base(message)
    {
    }

    internal InvalidAddressException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
