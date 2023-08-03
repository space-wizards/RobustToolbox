using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Robust.Shared.Network;

internal static class HappyEyeballsHttp
{
    // .NET does not implement Happy Eyeballs at the time of writing.
    // https://github.com/space-wizards/SS14.Launcher/issues/38
    // This is the workaround.
    //
    // Implementation taken from https://github.com/ppy/osu-framework/pull/4191/files

    public static SocketsHttpHandler CreateHttpHandler()
    {
        return new SocketsHttpHandler
        {
            ConnectCallback = OnConnect,
            AutomaticDecompression = DecompressionMethods.All,
        };
    }

    /// <summary>
    /// Whether IPv6 should be preferred. Value may change based on runtime failures.
    /// </summary>
    private static bool _useIPv6 = Socket.OSSupportsIPv6;

    /// <summary>
    /// Whether the initial IPv6 check has been performed (to determine whether v6 is available or not).
    /// </summary>
    private static bool _hasResolvedIPv6Availability;

    private const int FirstTryTimeout = 2000;

    private static async ValueTask<Stream> OnConnect(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        if (_useIPv6)
        {
            try
            {
                var localToken = cancellationToken;

                if (!_hasResolvedIPv6Availability)
                {
                    // to make things move fast, use a very low timeout for the initial ipv6 attempt.
                    var quickFailCts = new CancellationTokenSource(FirstTryTimeout);
                    var linkedTokenSource =
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, quickFailCts.Token);

                    localToken = linkedTokenSource.Token;
                }

                return await AttemptConnection(AddressFamily.InterNetworkV6, context, localToken);
            }
            catch
            {
                // very naively fallback to ipv4 permanently for this execution based on the response of the first connection attempt.
                // note that this may cause users to eventually get switched to ipv4 (on a random failure when they are switching networks, for instance)
                // but in the interest of keeping this implementation simple, this is acceptable.
                _useIPv6 = false;
            }
            finally
            {
                _hasResolvedIPv6Availability = true;
            }
        }

        // fallback to IPv4.
        return await AttemptConnection(AddressFamily.InterNetwork, context, cancellationToken);
    }

    private static async ValueTask<Stream> AttemptConnection(
        AddressFamily addressFamily,
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        // The following socket constructor will create a dual-mode socket on systems where IPV6 is available.
        var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            // Turn off Nagle's algorithm since it degrades performance in most HttpClient scenarios.
            NoDelay = true
        };

        try
        {
            await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
            // The stream should take the ownership of the underlying socket,
            // closing it when it's disposed.
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
