using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using SS14.Shared.Log;
using SS14.Shared.Utility;

namespace SS14.Shared.Network
{
    public partial class NetManager
    {
        private readonly Dictionary<NetConnection, (CancellationTokenRegistration, TaskCompletionSource<object>)>
            _awaitingStatusChange
                = new Dictionary<NetConnection, (CancellationTokenRegistration, TaskCompletionSource<object>)>();

        private readonly
            Dictionary<NetConnection, (CancellationTokenRegistration, TaskCompletionSource<NetIncomingMessage>)>
            _awaitingData =
                new Dictionary<NetConnection, (CancellationTokenRegistration, TaskCompletionSource<NetIncomingMessage>)
                >();

        /// <inheritdoc />
        public async void ClientConnect(string host, int port, string userNameRequest)
        {
            DebugTools.Assert(!IsServer, "Should never be called on the server.");
            DebugTools.Assert(_clientConnectionState == ClientConnectionState.NotConnecting);

            _clientConnectionState = ClientConnectionState.ResolvingHost;

            Logger.DebugS("net", "Attempting to connect to {0} port {1}", host, port);

            // Get list of potential IP addresses for the domain.
            var endPoints = await NetUtility.ResolveAsync(host);

            if (endPoints == null)
            {
                // TODO: Don't crash but inform the user.
                throw new InvalidOperationException();
            }

            // Try to get an IPv6 and IPv4 address.
            var ipv6 = endPoints.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetworkV6);
            var ipv4 = endPoints.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

            if (ipv4 == null && ipv6 == null)
            {
                // TODO: Don't crash but inform the user.
                throw new InvalidOperationException();
            }

            _clientConnectionState = ClientConnectionState.EstablishingConnection;

            IPAddress first;
            IPAddress second = null;
            if (ipv6 != null)
            {
                // If there's an IPv6 address try it first then the IPv4.
                first = ipv6;
                second = ipv4;
            }
            else
            {
                first = ipv4;
            }

            Logger.DebugS("net", "First attempt IP address is {0}, second attempt {1}", first, second);

            NetPeer CreatePeerForIp(IPAddress address)
            {
                var config = _getBaseNetPeerConfig();
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    config.LocalAddress = IPAddress.IPv6Any;
                }
                else
                {
                    config.LocalAddress = IPAddress.Any;
                }

                var peer = new NetPeer(config);
                peer.Start();
                _netPeers.Add(peer);
                return peer;
            }

            // Create first peer.
            var firstPeer = CreatePeerForIp(first);
            var firstConnection = firstPeer.Connect(new IPEndPoint(first, port));
            NetPeer secondPeer = null;
            NetConnection secondConnection = null;

            async Task ConnectSecondDelayed(CancellationToken cancellationToken)
            {
                DebugTools.AssertNotNull(second);
                // Connecting via second peer is delayed by 25ms to give an advantage to IPv6, if it works.
                await Task.Delay(25);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                secondPeer = CreatePeerForIp(second);
                secondConnection = secondPeer.Connect(new IPEndPoint(second, port));

                await AwaitStatusChange(secondConnection, cancellationToken);
            }

            NetPeer winningPeer;
            NetConnection winningConnection;
            if (second != null)
            {
                // We have two addresses to try.
                var cancellation = new CancellationTokenSource();
                var firstPeerChanged = AwaitStatusChange(firstConnection, cancellation.Token);
                var secondPeerChanged = ConnectSecondDelayed(cancellation.Token);

                var firstChange = await Task.WhenAny(firstPeerChanged, secondPeerChanged);

                if (firstChange == firstPeerChanged)
                {
                    Logger.DebugS("net", "First peer status changed.");
                    // First peer responded first.
                    if (firstConnection.Status == NetConnectionStatus.Connected)
                    {
                        // First peer won!
                        Logger.DebugS("net", "First peer succeeded.");
                        cancellation.Cancel();
                        if (secondPeer != null)
                        {
                            secondPeer.Shutdown("First connection attempt won.");
                            _netPeers.Remove(secondPeer);
                        }
                        winningPeer = firstPeer;
                        winningConnection = firstConnection;
                    }
                    else
                    {
                        // First peer failed, try the second one I guess.
                        Logger.DebugS("net", "First peer failed.");
                        firstPeer.Shutdown("You failed.");
                        _netPeers.Remove(firstPeer);
                        await secondPeerChanged;
                        winningPeer = secondPeer;
                        winningConnection = secondConnection;
                    }
                }
                else
                {
                    if (secondConnection.Status == NetConnectionStatus.Connected)
                    {
                        // Second peer won!
                        Logger.DebugS("net", "Second peer succeeded.");
                        cancellation.Cancel();
                        firstPeer.Shutdown("Second connection attempt won.");
                        _netPeers.Remove(firstPeer);
                        winningPeer = secondPeer;
                        winningConnection = secondConnection;
                    }
                    else
                    {
                        // First peer failed, try the second one I guess.
                        Logger.DebugS("net", "Second peer failed.");
                        secondPeer.Shutdown("You failed.");
                        _netPeers.Remove(secondPeer);
                        await firstPeerChanged;
                        winningPeer = firstPeer;
                        winningConnection = firstConnection;
                    }
                }
            }
            else
            {
                // Only one address to try. Pretty straight forward.
                await AwaitStatusChange(firstConnection);
                winningPeer = firstPeer;
                winningConnection = firstConnection;
            }

            // winningPeer can still be failed at this point.
            // If it is, neither succeeded. RIP.
            if (winningConnection.Status != NetConnectionStatus.Connected)
            {
                // TODO: Don't crash but inform the user.
                throw new InvalidOperationException();
            }

            _clientConnectionState = ClientConnectionState.Handshake;

            // We're connected start handshaking.

            var userNameRequestMsg = winningPeer.CreateMessage(userNameRequest);
            winningPeer.SendMessage(userNameRequestMsg, winningConnection, NetDeliveryMethod.ReliableOrdered);

            // Await response.
            var response = await AwaitData(winningConnection);
            var receivedUsername = response.ReadString();
            var channel = new NetChannel(this, winningConnection, new NetSessionId(receivedUsername));
            _channels.Add(winningConnection, channel);

            var confirmConnectionMsg = winningPeer.CreateMessage("ok");
            winningPeer.SendMessage(userNameRequestMsg, winningConnection, NetDeliveryMethod.ReliableOrdered);

            _clientConnectionState = ClientConnectionState.Connected;
            Logger.DebugS("net", "Handshake completed, connection established.");
        }

        private Task AwaitStatusChange(NetConnection connection, CancellationToken cancellationToken = default)
        {
            if (_awaitingStatusChange.ContainsKey(connection))
            {
                throw new InvalidOperationException();
            }

            var tcs = new TaskCompletionSource<object>();
            CancellationTokenRegistration reg = default;
            if (cancellationToken != default)
            {
                reg = cancellationToken.Register(() =>
                {
                    _awaitingStatusChange.Remove(connection);
                    tcs.TrySetCanceled();
                });
            }

            _awaitingStatusChange.Add(connection, (reg, tcs));
            return tcs.Task;
        }

        private Task<NetIncomingMessage> AwaitData(NetConnection connection, CancellationToken cancellationToken= default)
        {
            if (_awaitingData.ContainsKey(connection))
            {
                throw new InvalidOperationException("Cannot await data twice.");
            }

            var tcs = new TaskCompletionSource<NetIncomingMessage>();
            CancellationTokenRegistration reg = default;
            if (cancellationToken != default)
            {
                reg = cancellationToken.Register(() =>
                {
                    _awaitingData.Remove(connection);
                    tcs.TrySetCanceled();
                });
            }

            _awaitingData.Add(connection, (reg, tcs));
            return tcs.Task;
        }
    }
}
