using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Lidgren.Network;
using Robust.Shared.AuthLib;
using Robust.Shared.Log;
using Robust.Shared.Network.Messages.Handshake;
using Robust.Shared.Utility;
using SpaceWizards.Sodium;

namespace Robust.Shared.Network
{
    partial class NetManager
    {
        private static readonly string DisconnectReasonWrongKey = new NetDisconnectMessage("Token decryption failed.\nPlease reconnect to this server from the launcher.", true).Encode();

        private readonly byte[] _cryptoPrivateKey = new byte[CryptoBox.SecretKeyBytes];

        public byte[] CryptoPublicKey { get; } = new byte[CryptoBox.PublicKeyBytes];
        public AuthMode Auth { get; private set; }

        public Func<string, Task<NetUserId?>>? AssignUserIdCallback { get; set; }
        public IServerNetManager.NetApprovalDelegate? HandleApprovalCallback { get; set; }

        private void SAGenerateKeys()
        {
            CryptoBox.KeyPair(CryptoPublicKey, _cryptoPrivateKey);

            _authLogger.Debug("Public key is {0}", Convert.ToBase64String(CryptoPublicKey));
        }

        private async void HandleHandshake(NetPeerData peer, NetConnection connection)
        {
            try
            {
                _logger.Verbose($"{connection.RemoteEndPoint}: Starting handshake with peer ");

                _logger.Verbose($"{connection.RemoteEndPoint}: Awaiting MsgLoginStart");
                var incPacket = await AwaitData(connection);

                var msgLogin = new MsgLoginStart();
                msgLogin.ReadFromBuffer(incPacket, _serializer);

                var ip = connection.RemoteEndPoint.Address;
                var isLocal = IPAddress.IsLoopback(ip) && _config.GetCVar(CVars.AuthAllowLocal);
                var canAuth = msgLogin.CanAuth;
                var needPk = msgLogin.NeedPubKey;
                var authServer = _config.GetCVar(CVars.AuthServer);

                _logger.Verbose(
                    $"{connection.RemoteEndPoint}: Received MsgLoginStart. " +
                    $"canAuth: {canAuth}, needPk: {needPk}, username: {msgLogin.UserName}, encrypt: {msgLogin.Encrypt}");

                _logger.Verbose(
                    $"{connection.RemoteEndPoint}: Connection is specialized local? {isLocal} ");

                if (Auth == AuthMode.Required && !isLocal)
                {
                    if (!canAuth)
                    {
                        connection.Disconnect("Connecting to this server requires authentication");
                        return;
                    }
                }

                NetEncryption? encryption = null;
                NetUserData userData;
                LoginType type;
                var padSuccessMessage = true;

                if (canAuth && Auth != AuthMode.Disabled)
                {
                    _logger.Verbose(
                        $"{connection.RemoteEndPoint}: Initiating authentication");

                    var verifyToken = new byte[4];
                    RandomNumberGenerator.Fill(verifyToken);
                    var wantHwid = _config.GetCVar(CVars.NetHWId);
                    var msgEncReq = new MsgEncryptionRequest
                    {
                        PublicKey = needPk ? CryptoPublicKey : Array.Empty<byte>(),
                        VerifyToken = verifyToken,
                        WantHwid = wantHwid
                    };

                    var outMsgEncReq = peer.Peer.CreateMessage();
                    outMsgEncReq.Write(false);
                    outMsgEncReq.WritePadBits();
                    msgEncReq.WriteToBuffer(outMsgEncReq, _serializer);
                    peer.Peer.SendMessage(outMsgEncReq, connection, NetDeliveryMethod.ReliableOrdered);

                    _logger.Verbose(
                        $"{connection.RemoteEndPoint}: Awaiting MsgEncryptionResponse");

                    incPacket = await AwaitData(connection);

                    var msgEncResponse = new MsgEncryptionResponse();
                    msgEncResponse.ReadFromBuffer(incPacket, _serializer);

                    _logger.Verbose(
                        $"{connection.RemoteEndPoint}: Received MsgEncryptionResponse");

                    var encResp = new byte[verifyToken.Length + SharedKeyLength];
                    var ret = CryptoBox.SealOpen(
                        encResp,
                        msgEncResponse.SealedData,
                        CryptoPublicKey,
                        _cryptoPrivateKey);

                    if (!ret)
                    {
                        // Launcher gives the client the public RSA key of the server BUT
                        // that doesn't persist if the server restarts.
                        // In that case, the decrypt can fail here.
                        connection.Disconnect(DisconnectReasonWrongKey);
                        return;
                    }

                    // Data is [shared]+[verify]
                    var verifyTokenCheck = encResp[SharedKeyLength..];
                    var sharedSecret = encResp[..SharedKeyLength];

                    if (!verifyToken.AsSpan().SequenceEqual(verifyTokenCheck))
                    {
                        connection.Disconnect("Verify token is invalid");
                        return;
                    }

                    if (msgLogin.Encrypt)
                        encryption = new NetEncryption(sharedSecret, isServer: true);

                    _logger.Verbose(
                        $"{connection.RemoteEndPoint}: Checking with session server for auth hash...");

                    var authHashBytes = MakeAuthHash(sharedSecret, CryptoPublicKey!);
                    var authHash = Base64Helpers.ConvertToBase64Url(authHashBytes);

                    var url = $"{authServer}api/session/hasJoined?hash={authHash}&userId={msgEncResponse.UserId}";
                    var joinedRespJson = await _http.Client.GetFromJsonAsync<HasJoinedResponse>(url);

                    if (joinedRespJson is not {IsValid: true})
                    {
                        connection.Disconnect("Failed to validate login");
                        return;
                    }

                    _logger.Verbose(
                        $"{connection.RemoteEndPoint}: Auth hash passed. " +
                        $"User ID: {joinedRespJson.UserData!.UserId}, " +
                        $"Username: {joinedRespJson.UserData!.UserName}," +
                        $"Patron: {joinedRespJson.UserData.PatronTier}");

                    var userId = new NetUserId(joinedRespJson.UserData!.UserId);
                    ImmutableArray<ImmutableArray<byte>> modernHWIds = [
                        ..joinedRespJson.ConnectionData!.Hwids
                            .Select(h => ImmutableArray.Create(Convert.FromBase64String(h)))
                    ];
                    ImmutableArray<byte> legacyHwid = [..msgEncResponse.LegacyHwid];
                    if (!wantHwid)
                    {
                        // If the client somehow sends a HWID even if we didn't ask for one, ignore it.
                        modernHWIds = [];
                        legacyHwid = [];
                    }

                    userData = new NetUserData(userId, joinedRespJson.UserData.UserName)
                    {
                        PatronTier = joinedRespJson.UserData.PatronTier,
                        HWId = legacyHwid,
                        ModernHWIds = modernHWIds,
                        Trust = joinedRespJson.ConnectionData!.Trust
                    };
                    padSuccessMessage = false;
                    type = LoginType.LoggedIn;
                }
                else
                {
                    _logger.Verbose(
                        $"{connection.RemoteEndPoint}: Not doing authentication");

                    var reqUserName = msgLogin.UserName;

                    if (!UsernameHelpers.IsNameValid(reqUserName, out var reason))
                    {
                        connection.Disconnect($"Username is invalid ({reason.ToText()}).");
                        return;
                    }

                    // If auth is set to "optional" we need to avoid conflicts between real accounts and guests,
                    // so we explicitly prefix guests.
                    var origName = Auth == AuthMode.Disabled
                        ? reqUserName
                        : (isLocal ? $"localhost@{reqUserName}" : $"guest@{reqUserName}");
                    var name = origName;
                    var iterations = 1;

                    while (_assignedUsernames.ContainsKey(name))
                    {
                        // This is shit but I don't care.
                        name = $"{origName}_{++iterations}";
                    }

                    _logger.Verbose(
                        $"{connection.RemoteEndPoint}: Assigned name: {name}");

                    NetUserId userId;
                    (userId, type) = await AssignUserIdAsync(name);

                    _logger.Verbose(
                        $"{connection.RemoteEndPoint}: Assigned user ID: {userId}");

                    userData = new NetUserData(userId, name)
                    {
                        HWId = [],
                        ModernHWIds = []
                    };
                }

                _logger.Verbose(
                    $"{connection.RemoteEndPoint}: Login type: {type}");

                _logger.Verbose(
                    $"{connection.RemoteEndPoint}: Raising Connecting event");

                var endPoint = connection.RemoteEndPoint;
                var connect = await OnConnecting(endPoint, userData, type);
                if (connect.DenyReasonData is { } deny)
                {
                    var denyMsg = $"Connect denied: {deny.Text}";
                    var structured = new NetDisconnectMessage(denyMsg);
                    foreach (var (k, v) in deny.AdditionalProperties)
                    {
                        structured.Values[k] = v;
                    }
                    connection.Disconnect(structured.Encode());
                    return;
                }

                _logger.Verbose(
                    $"{connection.RemoteEndPoint}: Connecting event passed, client is IN");

                // Well they're in. Kick a connected client with the same GUID if we have to.
                if (_assignedUserIds.TryGetValue(userData.UserId, out var existing))
                {
                    _logger.Verbose(
                        $"{connection.RemoteEndPoint}: User was already connected in another connection, disconnecting");

                    if (_awaitingDisconnectToConnect.Contains(userData.UserId))
                    {
                        connection.Disconnect("Stop trying to connect multiple times at once.");
                        return;
                    }

                    _awaitingDisconnectToConnect.Add(userData.UserId);
                    try
                    {
                        existing.Disconnect("Another connection has been made with your account.");
                        // Have to wait until they're properly off the server to avoid any collisions.

                        _logger.Verbose(
                            $"{connection.RemoteEndPoint}: Awaiting for clean disconnect of previous client");

                        await AwaitDisconnectAsync(existing);

                        _logger.Verbose(
                            $"{connection.RemoteEndPoint}: Previous client disconnected");
                    }
                    finally
                    {
                        _awaitingDisconnectToConnect.Remove(userData.UserId);
                    }
                }

                if (connection.Status == NetConnectionStatus.Disconnecting ||
                    connection.Status == NetConnectionStatus.Disconnected)
                {
                    _logger.Info("{ConnectionEndpoint} ({UserId}/{UserName}) disconnected during handshake",
                        connection.RemoteEndPoint, userData.UserId, userData.UserName);

                    return;
                }

                _logger.Verbose($"{connection.RemoteEndPoint}: Sending MsgLoginSuccess");

                var msg = peer.Peer.CreateMessage();
                var msgResp = new MsgLoginSuccess
                {
                    UserData = userData,
                    Type = type
                };
                if (padSuccessMessage)
                {
                    msg.Write(true);
                    msg.WritePadBits();
                }

                msgResp.WriteToBuffer(msg, _serializer);
                encryption?.Encrypt(msg);
                peer.Peer.SendMessage(msg, connection, NetDeliveryMethod.ReliableOrdered);

                _logger.Info("Approved {ConnectionEndpoint} with username {Username} user ID {userId} into the server",
                    connection.RemoteEndPoint, userData.UserName, userData.UserId);

                // Handshake complete!
                HandleInitialHandshakeComplete(peer, connection, userData, encryption, type);
            }
            catch (ClientDisconnectedException)
            {
                _logger.Info($"Peer {NetUtility.ToHexString(connection.RemoteUniqueIdentifier)} disconnected while handshake was in-progress.");
            }
            catch (Exception e)
            {
                connection.Disconnect("Unknown server error occured during handshake.");
                _logger.Error("Exception during handshake with peer {0}:\n{1}",
                    NetUtility.ToHexString(connection.RemoteUniqueIdentifier), e);
            }
        }

        private async Task<(NetUserId, LoginType)> AssignUserIdAsync(string username)
        {
            if (AssignUserIdCallback == null)
            {
                goto unassigned;
            }

            var assigned = await AssignUserIdCallback(username);
            if (assigned != null)
            {
                return (assigned.Value, LoginType.GuestAssigned);
            }

            unassigned:
            // Just generate a random new GUID.
            var uid = new NetUserId(Guid.NewGuid());
            return (uid, LoginType.Guest);
        }

        private Task AwaitDisconnectAsync(NetConnection connection)
        {
            if (!_awaitingDisconnect.TryGetValue(connection, out var tcs))
            {
                tcs = new TaskCompletionSource<object?>();
                _awaitingDisconnect.Add(connection, tcs);
            }

            return tcs.Task;
        }

        private async void HandleApproval(NetIncomingMessage message)
        {
            DebugTools.Assert(message.SenderConnection != null);
            // TODO: Maybe preemptively refuse connections here in some cases?
            if (message.SenderConnection.Status != NetConnectionStatus.RespondedAwaitingApproval)
            {
                // This can happen if the approval message comes in after the state changes to disconnected.
                // In that case just ignore it.
                return;
            }

            if (HandleApprovalCallback != null)
            {
                var approval = await HandleApprovalCallback(new NetApprovalEventArgs(message.SenderConnection));

                if (!approval.IsApproved)
                {
                    message.SenderConnection.Deny(approval.DenyReason);
                    return;
                }
            }

            message.SenderConnection.Approve();
        }

        // ReSharper disable ClassNeverInstantiated.Local
        private sealed record HasJoinedResponse(bool IsValid, HasJoinedUserData? UserData, HasJoinedConnectionData? ConnectionData);
        private sealed record HasJoinedUserData(string UserName, Guid UserId, string? PatronTier);
        private sealed record HasJoinedConnectionData(string[] Hwids, float Trust);
        // ReSharper restore ClassNeverInstantiated.Local
    }
}
