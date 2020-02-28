using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Network
{
    public partial class NetManager
    {
        private CancellationTokenSource _cancelConnectTokenSource;

        private readonly
            Dictionary<NetConnection, (CancellationTokenRegistration reg, TaskCompletionSource<string> tcs)>
            _awaitingStatusChange
                = new Dictionary<NetConnection, (CancellationTokenRegistration, TaskCompletionSource<string>)>();

        private readonly
            Dictionary<NetConnection, (CancellationTokenRegistration, TaskCompletionSource<NetIncomingMessage>)>
            _awaitingData =
                new Dictionary<NetConnection, (CancellationTokenRegistration, TaskCompletionSource<NetIncomingMessage>)
                >();

        /// <inheritdoc />
        public async void ClientConnect(string host, int port, string userNameRequest)
        {
            DebugTools.Assert(!IsServer, "Should never be called on the server.");
            if (_clientConnectionState == ClientConnectionState.Connected)
            {
                throw new InvalidOperationException("The client is already connected to a server.");
            }

            if (_clientConnectionState != ClientConnectionState.NotConnecting)
            {
                throw new InvalidOperationException("A connect attempt is already in progress. Cancel it first.");
            }

            _cancelConnectTokenSource = new CancellationTokenSource();
            var mainCancelToken = _cancelConnectTokenSource.Token;
            _clientConnectionState = ClientConnectionState.ResolvingHost;

            Logger.DebugS("net", "Attempting to connect to {0} port {1}", host, port);

            // Get list of potential IP addresses for the domain.
            var endPoints = await ResolveDnsAsync(host);

            if (mainCancelToken.IsCancellationRequested)
            {
                _clientConnectionState = ClientConnectionState.NotConnecting;
                return;
            }

            if (endPoints == null)
            {
                OnConnectFailed($"Unable to resolve domain '{host}'");
                _clientConnectionState = ClientConnectionState.NotConnecting;
                return;
            }

            // Try to get an IPv6 and IPv4 address.
            var ipv6 = endPoints.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetworkV6);
            var ipv4 = endPoints.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

            if (ipv4 == null && ipv6 == null)
            {
                OnConnectFailed($"Domain '{host}' has no associated IP addresses");
                _clientConnectionState = ClientConnectionState.NotConnecting;
                return;
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
            string secondReason = null;

            async Task<string> AwaitNonInitStatusChange(NetConnection connection, CancellationToken cancellationToken)
            {
                NetConnectionStatus status;
                string reason;

                do
                {
                    reason = await AwaitStatusChange(connection, cancellationToken);
                    status = connection.Status;
                } while (status == NetConnectionStatus.InitiatedConnect);

                return reason;
            }

            async Task ConnectSecondDelayed(CancellationToken cancellationToken)
            {
                DebugTools.AssertNotNull(second);
                // Connecting via second peer is delayed by 25ms to give an advantage to IPv6, if it works.
                await Task.Delay(25, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                secondPeer = CreatePeerForIp(second);
                secondConnection = secondPeer.Connect(new IPEndPoint(second, port));

                secondReason = await AwaitNonInitStatusChange(secondConnection, cancellationToken);
            }

            NetPeer winningPeer;
            NetConnection winningConnection;
            string firstReason = null;
            try
            {
                if (second != null)
                {
                    // We have two addresses to try.
                    var cancellation = CancellationTokenSource.CreateLinkedTokenSource(mainCancelToken);
                    var firstPeerChanged = AwaitNonInitStatusChange(firstConnection, cancellation.Token);
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
                                _toCleanNetPeers.Add(secondPeer);
                            }

                            winningPeer = firstPeer;
                            winningConnection = firstConnection;
                        }
                        else
                        {
                            // First peer failed, try the second one I guess.
                            Logger.DebugS("net", "First peer failed.");
                            firstPeer.Shutdown("You failed.");
                            _toCleanNetPeers.Add(firstPeer);
                            firstReason = firstPeerChanged.Result;
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
                            _toCleanNetPeers.Add(firstPeer);
                            winningPeer = secondPeer;
                            winningConnection = secondConnection;
                        }
                        else
                        {
                            // First peer failed, try the second one I guess.
                            Logger.DebugS("net", "Second peer failed.");
                            secondPeer.Shutdown("You failed.");
                            _toCleanNetPeers.Add(secondPeer);
                            firstReason = await firstPeerChanged;
                            winningPeer = firstPeer;
                            winningConnection = firstConnection;
                        }
                    }
                }
                else
                {
                    // Only one address to try. Pretty straight forward.
                    firstReason = await AwaitNonInitStatusChange(firstConnection, mainCancelToken);
                    winningPeer = firstPeer;
                    winningConnection = firstConnection;
                }
            }
            catch (TaskCanceledException)
            {
                firstPeer.Shutdown("Cancelled");
                _toCleanNetPeers.Add(firstPeer);
                if (secondPeer != null)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    secondPeer.Shutdown("Cancelled");
                    _toCleanNetPeers.Add(secondPeer);
                }

                _clientConnectionState = ClientConnectionState.NotConnecting;
                return;
            }

            // winningPeer can still be failed at this point.
            // If it is, neither succeeded. RIP.
            if (winningConnection.Status != NetConnectionStatus.Connected)
            {
                winningPeer.Shutdown("You failed");
                _toCleanNetPeers.Add(winningPeer);
                OnConnectFailed(secondReason ?? firstReason);
                _clientConnectionState = ClientConnectionState.NotConnecting;
                return;
            }

            _clientConnectionState = ClientConnectionState.Handshake;

            // We're connected start handshaking.

            var userNameRequestMsg = winningPeer.CreateMessage(userNameRequest);
            winningPeer.SendMessage(userNameRequestMsg, winningConnection, NetDeliveryMethod.ReliableOrdered);

            try
            {
                // Await response.
                var response = await AwaitData(winningConnection, mainCancelToken);
                var receivedUsername = response.ReadString();
                var channel = new NetChannel(this, winningConnection, new NetSessionId(receivedUsername));
                _channels.Add(winningConnection, channel);

                var confirmConnectionMsg = winningPeer.CreateMessage("ok");
                winningPeer.SendMessage(confirmConnectionMsg, winningConnection, NetDeliveryMethod.ReliableOrdered);
            }
            catch (TaskCanceledException)
            {
                winningPeer.Shutdown("Cancelled");
                _toCleanNetPeers.Add(secondPeer);
                _clientConnectionState = ClientConnectionState.NotConnecting;
                return;
            }
            catch (Exception e)
            {
                OnConnectFailed(e.Message);
                Logger.ErrorS("net", "Exception during handshake: {0}", e);
                winningPeer.Shutdown("Something happened.");
                _toCleanNetPeers.Add(secondPeer);
                _clientConnectionState = ClientConnectionState.NotConnecting;
                return;
            }

            _clientConnectionState = ClientConnectionState.Connected;
            Logger.DebugS("net", "Handshake completed, connection established.");
        }

        private Task<string> AwaitStatusChange(NetConnection connection, CancellationToken cancellationToken = default)
        {
            if (_awaitingStatusChange.ContainsKey(connection))
            {
                throw new InvalidOperationException();
            }

            var tcs = new TaskCompletionSource<string>();
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

        private Task<NetIncomingMessage> AwaitData(NetConnection connection,
            CancellationToken cancellationToken = default)
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

        public static async Task<IPAddress[]> ResolveDnsAsync(string ipOrHost)
        {
            if (string.IsNullOrEmpty(ipOrHost))
            {
                throw new ArgumentException("Supplied string must not be empty", nameof(ipOrHost));
            }

            ipOrHost = ipOrHost.Trim();

            if (IPAddress.TryParse(ipOrHost, out var ipAddress))
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork
                    || ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    return new[] {ipAddress};
                }

                throw new ArgumentException("This method will not currently resolve other than IPv4 or IPv6 addresses");
            }

            try
            {
                var entry = await Dns.GetHostEntryAsync(ipOrHost);
                return entry.AddressList;
            }
            catch (SocketException)
            {
                return null;
            }
        }

    }
}
