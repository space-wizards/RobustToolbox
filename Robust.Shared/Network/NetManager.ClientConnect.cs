using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using Robust.Shared.Log;
using Robust.Shared.Network.Messages.Handshake;
using Robust.Shared.Utility;
using SpaceWizards.Sodium;

namespace Robust.Shared.Network
{
    public partial class NetManager
    {
        private CancellationTokenSource? _cancelConnectTokenSource;
        private ClientConnectionState _clientConnectState;

        public ClientConnectionState ClientConnectState
        {
            get => _clientConnectState;
            private set
            {
                _clientConnectState = value;
                ClientConnectStateChanged?.Invoke(value);
            }
        }

        public event Action<ClientConnectionState>? ClientConnectStateChanged;

        private readonly
            Dictionary<NetConnection, (CancellationTokenRegistration reg, TaskCompletionSource<string> tcs)>
            _awaitingStatusChange
                = new();

        private readonly
            Dictionary<NetConnection, (CancellationTokenRegistration, TaskCompletionSource<NetIncomingMessage>)>
            _awaitingData =
                new();


        /// <inheritdoc />
        public async void ClientConnect(string host, int port, string userNameRequest)
        {
            DebugTools.Assert(!IsServer, "Should never be called on the server.");
            if (ClientConnectState == ClientConnectionState.Connected)
            {
                throw new InvalidOperationException("The client is already connected to a server.");
            }

            if (ClientConnectState != ClientConnectionState.NotConnecting)
            {
                throw new InvalidOperationException("A connect attempt is already in progress. Cancel it first.");
            }

            _cancelConnectTokenSource = new CancellationTokenSource();
            var mainCancelToken = _cancelConnectTokenSource.Token;

            ClientConnectState = ClientConnectionState.ResolvingHost;

            Logger.DebugS("net", "Attempting to connect to {0} port {1}", host, port);

            var resolveResult = await CCResolveHost(host, mainCancelToken);
            if (resolveResult == null)
            {
                ClientConnectState = ClientConnectionState.NotConnecting;
                return;
            }

            var (first, second) = resolveResult.Value;

            ClientConnectState = ClientConnectionState.EstablishingConnection;

            Logger.DebugS("net", "First attempt IP address is {0}, second attempt {1}", first, second);

            var result = await CCHappyEyeballs(port, first, second, mainCancelToken);

            if (result == null)
            {
                ClientConnectState = ClientConnectionState.NotConnecting;
                return;
            }

            var (winningPeer, winningConnection) = result.Value;

            ClientConnectState = ClientConnectionState.Handshake;

            // We're connected start handshaking.

            try
            {
                await CCDoHandshake(winningPeer, winningConnection, userNameRequest, mainCancelToken);
            }
            catch (TaskCanceledException)
            {
                winningPeer.Peer.Shutdown("Cancelled");
                _toCleanNetPeers.Add(winningPeer.Peer);
                ClientConnectState = ClientConnectionState.NotConnecting;
                return;
            }
            catch (Exception e)
            {
                OnConnectFailed(e.Message);
                Logger.ErrorS("net", "Exception during handshake: {0}", e);
                winningPeer.Peer.Shutdown("Something happened.");
                _toCleanNetPeers.Add(winningPeer.Peer);
                ClientConnectState = ClientConnectionState.NotConnecting;
                return;
            }

            ClientConnectState = ClientConnectionState.Connected;
            Logger.DebugS("net", "Handshake completed, connection established.");
        }

        private async Task CCDoHandshake(NetPeerData peer, NetConnection connection, string userNameRequest,
            CancellationToken cancel)
        {
            var encrypt = _config.GetCVar(CVars.NetEncrypt);
            var authToken = _authManager.Token;
            var pubKey = _authManager.PubKey;
            var authServer = _authManager.Server;
            var userId = _authManager.UserId;

            var hasPubKey = !string.IsNullOrEmpty(pubKey);
            var authenticate = !string.IsNullOrEmpty(authToken);

            var hwId = ImmutableArray.Create(HWId.Calc());
            var msgLogin = new MsgLoginStart
            {
                UserName = userNameRequest,
                CanAuth = authenticate,
                NeedPubKey = !hasPubKey,
                HWId = hwId,
                Encrypt = encrypt
            };

            var outLoginMsg = peer.Peer.CreateMessage();
            msgLogin.WriteToBuffer(outLoginMsg);
            peer.Peer.SendMessage(outLoginMsg, connection, NetDeliveryMethod.ReliableOrdered);

            NetEncryption? encryption = null;
            var response = await AwaitData(connection, cancel);
            var loginSuccess = response.ReadBoolean();
            response.ReadPadBits();
            if (!loginSuccess)
            {
                // Need to authenticate, packet is MsgEncryptionRequest
                var encRequest = new MsgEncryptionRequest();
                encRequest.ReadFromBuffer(response);

                var sharedSecret = new byte[SharedKeyLength];
                RandomNumberGenerator.Fill(sharedSecret);

                if (encrypt)
                    encryption = new NetEncryption(sharedSecret, isServer: false);

                byte[] keyBytes;
                if (hasPubKey)
                {
                    // public key provided by launcher.
                    keyBytes = Convert.FromBase64String(pubKey!);
                }
                else
                {
                    // public key is gotten from handshake.
                    keyBytes = encRequest.PublicKey;
                }

                if (keyBytes.Length != CryptoBox.PublicKeyBytes)
                {
                    connection.Disconnect("Invalid public key length");
                    return;
                }

                // Data is [shared]+[verify]
                var data = new byte[sharedSecret.Length + encRequest.VerifyToken.Length];
                sharedSecret.CopyTo(data.AsSpan());
                encRequest.VerifyToken.CopyTo(data.AsSpan(sharedSecret.Length));

                var sealedData = CryptoBox.Seal(data, keyBytes);

                var authHashBytes = MakeAuthHash(sharedSecret, keyBytes);
                var authHash = Convert.ToBase64String(authHashBytes);

                var joinReq = new JoinRequest(authHash);
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SS14Auth", authToken);
                var joinResp = await httpClient.PostAsJsonAsync(authServer + "api/session/join", joinReq, cancel);

                joinResp.EnsureSuccessStatusCode();

                var encryptionResponse = new MsgEncryptionResponse
                {
                    SealedData = sealedData,
                    UserId = userId!.Value.UserId
                };

                var outEncRespMsg = peer.Peer.CreateMessage();
                encryptionResponse.WriteToBuffer(outEncRespMsg);
                peer.Peer.SendMessage(outEncRespMsg, connection, NetDeliveryMethod.ReliableOrdered);

                // Expect login success here.
                response = await AwaitData(connection, cancel);
                encryption?.Decrypt(response);
            }

            var msgSuc = new MsgLoginSuccess();
            msgSuc.ReadFromBuffer(response);

            var channel = new NetChannel(this, connection, msgSuc.UserData with { HWId = hwId }, msgSuc.Type);
            _channels.Add(connection, channel);
            peer.AddChannel(channel);

            _clientEncryption = encryption;
        }

        private static byte[] MakeAuthHash(byte[] sharedSecret, byte[] pkBytes)
        {
            // Logger.DebugS("auth", "shared: {0}, pk: {1}", Convert.ToBase64String(sharedSecret), Convert.ToBase64String(pkBytes));

            var incHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            incHash.AppendData(sharedSecret);
            incHash.AppendData(pkBytes);
            return incHash.GetHashAndReset();
        }

        private async Task<(IPAddress first, IPAddress? second)?>
            CCResolveHost(string host, CancellationToken mainCancelToken)
        {
            // Get list of potential IP addresses for the domain.
            var endPoints = await ResolveDnsAsync(host);

            if (mainCancelToken.IsCancellationRequested)
            {
                return null;
            }

            if (endPoints == null)
            {
                OnConnectFailed($"Unable to resolve domain '{host}'");
                return null;
            }

            // Try to get an IPv6 and IPv4 address.
            var ipv6 = endPoints.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetworkV6);
            var ipv4 = endPoints.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

            if (ipv4 == null && ipv6 == null)
            {
                OnConnectFailed($"Domain '{host}' has no associated IP addresses");
                return null;
            }

            IPAddress first;
            IPAddress? second = null;
            if (ipv6 != null)
            {
                // If there's an IPv6 address try it first then the IPv4.
                first = ipv6;
                second = ipv4;
            }
            else
            {
                first = ipv4!;
            }

            return (first, second);
        }

        private async Task<(NetPeerData winningPeer, NetConnection winningConnection)?>
            CCHappyEyeballs(int port, IPAddress first, IPAddress? second, CancellationToken mainCancelToken)
        {
            NetPeerData CreatePeerForIp(IPAddress address)
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
                var data = new NetPeerData(peer);
                _netPeers.Add(data);
                return data;
            }

            // Create first peer.
            var firstPeer = CreatePeerForIp(first);
            var firstConnection = firstPeer.Peer.Connect(new IPEndPoint(first, port));
            NetPeerData? secondPeer = null;
            NetConnection? secondConnection = null;
            string? secondReason = null;

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
                secondConnection = secondPeer.Peer.Connect(new IPEndPoint(second, port));

                secondReason = await AwaitNonInitStatusChange(secondConnection, cancellationToken);
            }

            NetPeerData? winningPeer;
            NetConnection? winningConnection;
            string? firstReason = null;
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
                                secondPeer.Peer.Shutdown("First connection attempt won.");
                                _toCleanNetPeers.Add(secondPeer.Peer);
                            }

                            winningPeer = firstPeer;
                            winningConnection = firstConnection;
                        }
                        else
                        {
                            // First peer failed, try the second one I guess.
                            Logger.DebugS("net", "First peer failed.");
                            firstPeer.Peer.Shutdown("You failed.");
                            _toCleanNetPeers.Add(firstPeer.Peer);
                            firstReason = firstPeerChanged.Result;
                            await secondPeerChanged;
                            winningPeer = secondPeer;
                            winningConnection = secondConnection;
                        }
                    }
                    else
                    {
                        if (secondConnection!.Status == NetConnectionStatus.Connected)
                        {
                            // Second peer won!
                            Logger.DebugS("net", "Second peer succeeded.");
                            cancellation.Cancel();
                            firstPeer.Peer.Shutdown("Second connection attempt won.");
                            _toCleanNetPeers.Add(firstPeer.Peer);
                            winningPeer = secondPeer;
                            winningConnection = secondConnection;
                        }
                        else
                        {
                            // First peer failed, try the second one I guess.
                            Logger.DebugS("net", "Second peer failed.");
                            secondPeer!.Peer.Shutdown("You failed.");
                            _toCleanNetPeers.Add(secondPeer.Peer);
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
                firstPeer.Peer.Shutdown("Cancelled");
                _toCleanNetPeers.Add(firstPeer.Peer);
                if (secondPeer != null)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    secondPeer.Peer.Shutdown("Cancelled");
                    _toCleanNetPeers.Add(secondPeer.Peer);
                }

                return null;
            }

            // winningPeer can still be failed at this point.
            // If it is, neither succeeded. RIP.
            if (winningConnection!.Status != NetConnectionStatus.Connected)
            {
                winningPeer!.Peer.Shutdown("You failed");
                _toCleanNetPeers.Add(winningPeer.Peer);
                OnConnectFailed((secondReason ?? firstReason)!);
                return null;
            }

            return (winningPeer!, winningConnection);
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

            DebugTools.Assert(!_channels.ContainsKey(connection),
                "AwaitData cannot be used once a proper channel for the connection has been constructed, as it does not support encryption.");

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

        public static async Task<IPAddress[]?> ResolveDnsAsync(string ipOrHost)
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

        private sealed record JoinRequest(string Hash);
    }
}
