using System;
using System.Collections.Generic;
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

            _logger.Debug("Attempting to connect to {0} port {1}", host, port);

            var resolveResult = await CCResolveHost(host, mainCancelToken);
            if (resolveResult == null)
            {
                ClientConnectState = ClientConnectionState.NotConnecting;
                return;
            }

            var (first, second) = resolveResult.Value;

            ClientConnectState = ClientConnectionState.EstablishingConnection;

            _logger.Debug("First attempt IP address is {0}, second attempt {1}", first, second);

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
            catch (OperationCanceledException)
            {
                winningPeer.Peer.Shutdown("Cancelled");
                _toCleanNetPeers.Add(winningPeer.Peer);
                ClientConnectState = ClientConnectionState.NotConnecting;
                return;
            }
            catch (Exception e)
            {
                OnConnectFailed(e.Message);
                _logger.Error("Exception during handshake: {0}", e);
                winningPeer.Peer.Shutdown("Something happened.");
                _toCleanNetPeers.Add(winningPeer.Peer);
                ClientConnectState = ClientConnectionState.NotConnecting;
                return;
            }

            DebugTools.Assert(ChannelCount > 0 && winningPeer.Channels.Count > 0);
            ClientConnectState = ClientConnectionState.Connected;
            _logger.Debug("Handshake completed, connection established.");
        }

        private async Task CCDoHandshake(
            NetPeerData peer,
            NetConnection connection,
            string userNameRequest,
            CancellationToken cancel)
        {
            var encrypt = _config.GetCVar(CVars.NetEncrypt);
            var authToken = _authManager.Token;
            var pubKey = _authManager.PubKey;
            var authServer = _authManager.Server;
            var userId = _authManager.UserId;

            var hasPubKey = !string.IsNullOrEmpty(pubKey);
            var authenticate = !string.IsNullOrEmpty(authToken);

            byte[] legacyHwid = [];

            var msgLogin = new MsgLoginStart
            {
                UserName = userNameRequest,
                CanAuth = authenticate,
                NeedPubKey = !hasPubKey,
                Encrypt = encrypt
            };

            var outLoginMsg = peer.Peer.CreateMessage();
            msgLogin.WriteToBuffer(outLoginMsg, _serializer);
            peer.Peer.SendMessage(outLoginMsg, connection, NetDeliveryMethod.ReliableOrdered);

            NetEncryption? encryption = null;
            var response = await AwaitData(connection, cancel);
            var loginSuccess = response.ReadBoolean();
            response.ReadPadBits();
            if (!loginSuccess)
            {
                // Need to authenticate, packet is MsgEncryptionRequest
                var encRequest = new MsgEncryptionRequest();
                encRequest.ReadFromBuffer(response, _serializer);

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
                    var msg = $"Invalid public key length. Expected {CryptoBox.PublicKeyBytes}, but was {keyBytes.Length}.";
                    connection.Disconnect(msg);
                    throw new Exception(msg);
                }

                // Data is [shared]+[verify]
                var data = new byte[sharedSecret.Length + encRequest.VerifyToken.Length];
                sharedSecret.CopyTo(data.AsSpan());
                encRequest.VerifyToken.CopyTo(data.AsSpan(sharedSecret.Length));

                var sealedData = CryptoBox.Seal(data, keyBytes);

                var authHashBytes = MakeAuthHash(sharedSecret, keyBytes);
                var authHash = Convert.ToBase64String(authHashBytes);

                byte[]? modernHwid = null;
                if (_authManager.AllowHwid && encRequest.WantHwid)
                {
                    legacyHwid = _hwId.GetLegacy();
                    modernHwid = _hwId.GetModern();
                }

                var joinReq = new JoinRequest(authHash, Base64Helpers.ToBase64Nullable(modernHwid));
                var request = new HttpRequestMessage(HttpMethod.Post, authServer + "api/session/join");
                request.Content = JsonContent.Create(joinReq);
                request.Headers.Authorization = new AuthenticationHeaderValue("SS14Auth", authToken);
                var joinResp = await _http.Client.SendAsync(request, cancel);

                joinResp.EnsureSuccessStatusCode();

                var encryptionResponse = new MsgEncryptionResponse
                {
                    SealedData = sealedData,
                    UserId = userId!.Value.UserId,
                    LegacyHwid = legacyHwid
                };

                var outEncRespMsg = peer.Peer.CreateMessage();
                encryptionResponse.WriteToBuffer(outEncRespMsg, _serializer);
                peer.Peer.SendMessage(outEncRespMsg, connection, NetDeliveryMethod.ReliableOrdered);

                // Expect login success here.
                response = await AwaitData(connection, cancel);
                encryption?.Decrypt(response);
            }

            var msgSuc = new MsgLoginSuccess();
            msgSuc.ReadFromBuffer(response, _serializer);

            var channel = new NetChannel(this, connection, msgSuc.UserData with { HWId = [..legacyHwid] }, msgSuc.Type);
            _channels.Add(connection, channel);
            peer.AddChannel(channel);

            channel.Encryption = encryption;
            SetupEncryptionChannel(channel);
        }

        private byte[] MakeAuthHash(byte[] sharedSecret, byte[] pkBytes)
        {
            // _authLogger.Debug("auth", "shared: {0}, pk: {1}", Convert.ToBase64String(sharedSecret), Convert.ToBase64String(pkBytes));

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
            // Try to establish a connection with an IP address and wait for it to either connect or fail
            // Returns a disposable wrapper around the peer/connection because ParallelTask
            async Task<ConnectionAttempt> AttemptConnection(IPAddress address, CancellationToken cancel)
            {
                var config = _getBaseNetPeerConfig();
                config.LocalAddress = address.AddressFamily == AddressFamily.InterNetworkV6
                    ? IPAddress.IPv6Any
                    : IPAddress.Any;

                var peer = new NetPeer(config);
                peer.Start();
                var peerData = new NetPeerData(peer);
                _netPeers.Add(peerData);

                var connection = peer.Connect(new IPEndPoint(address, port));

                try
                {
                    // We need AwaitNonInitStatusChange to properly handle connection state transitions
                    var reason = await AwaitNonInitStatusChange(connection, cancel);

                    if (connection.Status != NetConnectionStatus.Connected)
                    {
                        // Connection failed, clean up and yeet an exception
                        peer.Shutdown(reason);
                        _toCleanNetPeers.Add(peer);
                        throw new Exception($"Connection failed: {reason}");
                    }

                    return new ConnectionAttempt(peerData, connection, this);
                }
                catch (Exception)
                {
                    // Something went wrong!
                    peer.Shutdown("Connection attempt failed");
                    _toCleanNetPeers.Add(peer);
                    throw;
                }
            }

            // Waits for a connection's status to change from InitiatedConnect to anything else
            async Task<string> AwaitNonInitStatusChange(NetConnection connection, CancellationToken cancellationToken)
            {
                string reason;
                NetConnectionStatus status;
                do
                {
                    reason = await AwaitStatusChange(connection, cancellationToken);
                    status = connection.Status;
                } while (status == NetConnectionStatus.InitiatedConnect);

                return reason;
            }

            try
            {
                // Create list of IPs to try
                var addresses = second != null
                    ? new[] { first, second }
                    : new[] { first };

                // Use ParallelTask to handle the connection attempts
                var delay = TimeSpan.FromSeconds(_config.GetCVar(CVars.NetHappyEyeballsDelay));
                var (result, _) = await HappyEyeballsHttp.ParallelTask(
                    addresses.Length,
                    (i, token) => AttemptConnection(addresses[i], token),
                    delay,
                    mainCancelToken);

                return (result.Peer, result.Connection);
            }
            catch (OperationCanceledException)
            {
                // Connection attempt was cancelled, nothing to see here
                OnConnectFailed("Connection attempt cancelled.");
                return null;
            }
            catch (AggregateException ae)
            {
                // ParallelTask throws AggregateException with all connection failures
                // We just take the first one
                var message = ae.InnerExceptions.First().Message;
                OnConnectFailed(message);
                return null;
            }
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

        private Task<NetIncomingMessage> AwaitData(
            NetConnection connection,
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

        private sealed record JoinRequest(string Hash, string? Hwid);

        private sealed class ConnectionAttempt(NetPeerData peer, NetConnection connection, NetManager netManager) : IDisposable
        {
            public NetPeerData Peer { get; } = peer;
            public NetConnection Connection { get; } = connection;

            public void Dispose()
            {
                Peer.Peer.Shutdown("Disposing unused connection attempt");
                netManager._toCleanNetPeers.Add(Peer.Peer);
            }
        }
    }
}
