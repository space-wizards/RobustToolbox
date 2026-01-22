using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Utility;

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
        // Am I going to check if that means they could modify it after the
        // local address check?
        // No, so we copy the original string instead just in case
        // !!FUN!!
        uri = new Uri(uri.OriginalString);
        await ThrowIfLocalAddress(uri);
        return await _client.GetStreamAsync(uri, cancel);
    }

    public async Task<string> GetStringAsync(Uri uri, CancellationToken cancel = default)
    {
        uri = new Uri(uri.OriginalString);
        await ThrowIfLocalAddress(uri);
        return await _client.GetStringAsync(uri, cancel);
    }

    public async Task<T?> GetFromJsonAsync<T>(Uri uri, CancellationToken cancel = default)
    {
        uri = new Uri(uri.OriginalString);
        await ThrowIfLocalAddress(uri);
        return await _client.GetFromJsonAsync<T>(uri, cancel);
    }

    private async Task ThrowIfLocalAddress(Uri uri)
    {
        if (IPAddress.TryParse(uri.Host, out var ip))
            ThrowIfLocalAddress(ip);

        var addresses = await Dns.GetHostAddressesAsync(uri.Host);
        foreach (var dnsIP in addresses)
        {
            ThrowIfLocalAddress(dnsIP);
        }
    }

    private void ThrowIfLocalAddress(IPAddress ip)
    {
        // IPv4
        var ipv4 = ip.ToString()
            .Split(".")
            .Select(s => (int?) (int.TryParse(s, out var i) ? i : null))
            .Where(i => i != null)
            .ToArray();
        ipv4.TryGetValue(0, out var first);
        ipv4.TryGetValue(1, out var second);
        switch (first)
        {
            case 10:
            case 192 when second == 168:
            case 172 when second is >= 16 and <= 31:
                ThrowLocalAddressException(ip);
                break;
        }

        // IPv6
        if (IPAddress.IsLoopback(ip) ||
            ip.IsIPv6LinkLocal ||
            ip.IsIPv6SiteLocal ||
            ip.IsIPv6UniqueLocal)
        {
            ThrowLocalAddressException(ip);
        }
    }

    private void ThrowLocalAddressException(IPAddress ip)
    {
        throw new InvalidAddressException($"{ip.ToString()} is a local address");
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
