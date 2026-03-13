using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Utility;

namespace Robust.Shared.Network;

public sealed class HttpManager : IHttpManagerInternal
{
    // From miniupnpc
    private static readonly (int ip, int mask)[] ReservedRangesIpv4 =
    [
        // @formatter:off
		(Ipv4(0,   0,   0,   0), 8 ), // RFC1122 "This host on this network"
		(Ipv4(10,  0,   0,   0), 8 ), // RFC1918 Private-Use
		(Ipv4(100, 64,  0,   0), 10), // RFC6598 Shared Address Space
		(Ipv4(127, 0,   0,   0), 8 ), // RFC1122 Loopback
		(Ipv4(169, 254, 0,   0), 16), // RFC3927 Link-Local
		(Ipv4(172, 16,  0,   0), 12), // RFC1918 Private-Use
		(Ipv4(192, 0,   0,   0), 24), // RFC6890 IETF Protocol Assignments
		(Ipv4(192, 0,   2,   0), 24), // RFC5737 Documentation (TEST-NET-1)
		(Ipv4(192, 31,  196, 0), 24), // RFC7535 AS112-v4
		(Ipv4(192, 52,  193, 0), 24), // RFC7450 AMT
		(Ipv4(192, 88,  99,  0), 24), // RFC7526 6to4 Relay Anycast
		(Ipv4(192, 168, 0,   0), 16), // RFC1918 Private-Use
		(Ipv4(192, 175, 48,  0), 24), // RFC7534 Direct Delegation AS112 Service
		(Ipv4(198, 18,  0,   0), 15), // RFC2544 Benchmarking
		(Ipv4(198, 51,  100, 0), 24), // RFC5737 Documentation (TEST-NET-2)
		(Ipv4(203, 0,   113, 0), 24), // RFC5737 Documentation (TEST-NET-3)
		(Ipv4(224, 0,   0,   0), 4 ), // RFC1112 Multicast
		(Ipv4(240, 0,   0,   0), 4 ), // RFC1112 Reserved for Future Use + RFC919 Limited Broadcast
        // @formatter:on
    ];

    private static readonly (UInt128 ip, int mask)[] ReservedRangesIpv6 =
    [
        (ToAddressBytes("::1"), 128), // "This host on this network"
        (ToAddressBytes("::ffff:0:0"), 96), // IPv4-mapped addresses
        (ToAddressBytes("::ffff:0:0:0"), 96), // IPv4-translated addresses
        (ToAddressBytes("64:ff9b:1::"), 48), // IPv4/IPv6 translation
        (ToAddressBytes("100::"), 64), // Discard prefix
        (ToAddressBytes("2001:20::"), 28), // ORCHIDv2
        (ToAddressBytes("2001:db8::"), 32), // Addresses used in documentation and example source code
        (ToAddressBytes("3fff::"), 20), // Addresses used in documentation and example source code
        (ToAddressBytes("5f00::"), 16), // IPv6 Segment Routing (SRv6)
        (ToAddressBytes("fc00::"), 7), // Unique local address
    ];

    private readonly HttpClient _client;

    public HttpManager()
    {
        _client = new HttpClient(HappyEyeballsHttp.CreateHttpHandler());
        HttpClientUserAgent.AddUserAgent(_client);
    }

    void IHttpManagerInternal.Shutdown()
    {
        _client.CancelPendingRequests();
    }

    private async Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancel = default)
    {
        // Uri can be inherited which means it could be inherited by the user
        // Am I going to check if that means they could modify it after the
        // local address check?
        // No, so we copy the original string instead just in case
        // !!FUN!!
        uri = new Uri(uri.OriginalString);
        var response = await _client.GetAsync(uri, cancel);
        await ThrowIfLocalUri(response);

        response.EnsureSuccessStatusCode();
        return response;
    }

    public async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancel = default)
    {
        var response = await GetAsync(uri, cancel);
        return await response.Content.ReadAsStreamAsync(cancel);
    }

    public async Task<string> GetStringAsync(Uri uri, CancellationToken cancel = default)
    {
        var response = await GetAsync(uri, cancel);
        return await response.Content.ReadAsStringAsync(cancel);
    }

    public async Task<byte[]> GetByteArrayAsync(Uri uri, CancellationToken cancel = default)
    {
        var response = await GetAsync(uri, cancel);
        return await response.Content.ReadAsByteArrayAsync(cancel);
    }

    public async Task<T?> GetFromJsonAsync<T>(Uri uri, CancellationToken cancel = default)
    {
        var response = await GetAsync(uri, cancel);
        return await response.Content.ReadFromJsonAsync<T>(cancel);
    }

    public async Task CopyToAsync(Uri uri, Stream stream, CancellationToken cancel = default)
    {
        var response = await GetAsync(uri, cancel);
        await response.Content.CopyToAsync(stream, cancel);
    }

    // Stolen from Lidgren.Network (Space Wizards Edition) (NetReservedAddress.cs)
    // Modified with IPV6 on top
    private static int Ipv4(byte a, byte b, byte c, byte d)
    {
        return (a << 24) | (b << 16) | (c << 8) | d;
    }

    private static UInt128 ToAddressBytes(string ip)
    {
        return BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse(ip).GetAddressBytes());
    }

    private static bool IsAddressReservedIpv4(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        Span<byte> ipBitsByte = stackalloc byte[4];
        address.TryWriteBytes(ipBitsByte, out _);
        var ipBits = BinaryPrimitives.ReadInt32BigEndian(ipBitsByte);

        foreach (var (reservedIp, maskBits) in ReservedRangesIpv4)
        {
            var mask = uint.MaxValue << (32 - maskBits);
            if ((ipBits & mask) == (reservedIp & mask))
                return true;
        }

        return false;
    }

    private static bool IsAddressReservedIpv6(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetworkV6)
            return false;

        if (address.IsIPv4MappedToIPv6)
            return IsAddressReservedIpv4(address.MapToIPv4());

        Span<byte> ipBitsByte = stackalloc byte[16];
        address.TryWriteBytes(ipBitsByte, out _);
        var ipBits = BinaryPrimitives.ReadInt128BigEndian(ipBitsByte);

        foreach (var (reservedIp, maskBits) in ReservedRangesIpv6)
        {
            var mask = UInt128.MaxValue << (128 - maskBits);
            if (((UInt128) ipBits & mask ) == (reservedIp & mask))
                return true;
        }

        return false;
    }

    private async Task ThrowIfLocalUri(HttpResponseMessage message)
    {
        if (message.RequestMessage?.RequestUri is not { } uri)
            throw new NullReferenceException("Response RequestUri is null");

        await ThrowIfLocalUri(uri);
    }

    internal async Task ThrowIfLocalUri(Uri uri)
    {
        if (IPAddress.TryParse(uri.Host, out var ip))
            ThrowIfLocalIP(ip);

        var addresses = await Dns.GetHostAddressesAsync(uri.Host);
        foreach (var dnsIP in addresses)
        {
            ThrowIfLocalIP(dnsIP);
        }
    }

    private void ThrowIfLocalIP(IPAddress ip)
    {
        if (IsAddressReservedIpv4(ip) || IsAddressReservedIpv6(ip))
            ThrowLocalAddressException(ip);
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
