using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Lidgren.Network;
using Newtonsoft.Json;
using Robust.Shared.Log;
using Robust.Shared.Network.Messages;
using Robust.Shared.Utility;
using UsernameHelpers = Robust.Shared.AuthLib.UsernameHelpers;

namespace Robust.Shared.Network
{
    partial class NetManager
    {
        private const int RsaKeySize = 2048;

        private RSA? _authRsaPrivateKey;

        public byte[]? RsaPublicKey { get; private set; }
        public AuthMode Auth { get; private set; }

        public Func<string, Task<NetUserId?>>? AssignUserIdCallback { get; set; }
        public IServerNetManager.NetApprovalDelegate? HandleApprovalCallback { get; set; }

        private void SAGenerateRsaKeys()
        {
            _authRsaPrivateKey = RSA.Create(RsaKeySize);
            RsaPublicKey = _authRsaPrivateKey.ExportRSAPublicKey();

            /*
            Logger.DebugS("auth", "Private RSA key is {0}",
                Convert.ToBase64String(_authRsaPrivateKey.ExportRSAPrivateKey()));
            */
            Logger.DebugS("auth", "Public RSA key is {0}", Convert.ToBase64String(RsaPublicKey));
        }

        private async void HandleHandshake(NetPeerData peer, NetConnection connection)
        {
            try
            {
                var incPacket = await AwaitData(connection);

                var msgLogin = new MsgLoginStart();
                msgLogin.ReadFromBuffer(incPacket);

                var ip = connection.RemoteEndPoint.Address;
                var isLocal = IPAddress.IsLoopback(ip) && _config.GetCVar(CVars.AuthAllowLocal);
                var canAuth = msgLogin.CanAuth;
                var needPk = msgLogin.NeedPubKey;
                var authServer = _config.GetCVar(CVars.AuthServer);

                if (Auth == AuthMode.Required && !isLocal)
                {
                    if (!canAuth)
                    {
                        connection.Disconnect("Connecting to this server requires authentication");
                        return;
                    }
                }

                NetEncryption? encryption = null;
                NetUserId userId;
                string userName;
                LoginType type;
                var padSuccessMessage = true;

                if (canAuth && Auth != AuthMode.Disabled)
                {
                    var verifyToken = new byte[4];
                    RandomNumberGenerator.Fill(verifyToken);
                    var msgEncReq = new MsgEncryptionRequest
                    {
                        PublicKey = needPk ? RsaPublicKey : Array.Empty<byte>(),
                        VerifyToken = verifyToken
                    };

                    var outMsgEncReq = peer.Peer.CreateMessage();
                    outMsgEncReq.Write(false);
                    outMsgEncReq.WritePadBits();
                    msgEncReq.WriteToBuffer(outMsgEncReq);
                    peer.Peer.SendMessage(outMsgEncReq, connection, NetDeliveryMethod.ReliableOrdered);

                    incPacket = await AwaitData(connection);

                    var msgEncResponse = new MsgEncryptionResponse();
                    msgEncResponse.ReadFromBuffer(incPacket);

                    byte[] verifyTokenCheck;
                    byte[] sharedSecret;
                    try
                    {
                        verifyTokenCheck = _authRsaPrivateKey!.Decrypt(
                            msgEncResponse.VerifyToken,
                            RSAEncryptionPadding.OaepSHA256);
                        sharedSecret = _authRsaPrivateKey!.Decrypt(
                            msgEncResponse.SharedSecret,
                            RSAEncryptionPadding.OaepSHA256);
                    }
                    catch (CryptographicException)
                    {
                        // Launcher gives the client the public RSA key of the server BUT
                        // that doesn't persist if the server restarts.
                        // In that case, the decrypt can fail here.
                        connection.Disconnect("Token decryption failed.\nPlease reconnect to this server from the launcher.");
                        return;
                    }

                    if (!verifyToken.SequenceEqual(verifyTokenCheck))
                    {
                        connection.Disconnect("Verify token is invalid");
                        return;
                    }

                    encryption = new NetAESEncryption(peer.Peer, sharedSecret, 0, sharedSecret.Length);

                    var authHashBytes = MakeAuthHash(sharedSecret, RsaPublicKey!);
                    var authHash = Base64Helpers.ConvertToBase64Url(authHashBytes);

                    var client = new HttpClient();
                    var url = $"{authServer}api/session/hasJoined?hash={authHash}&userId={msgEncResponse.UserId}";
                    var joinedResp = await client.GetAsync(url);

                    joinedResp.EnsureSuccessStatusCode();

                    var joinedRespJson = JsonConvert.DeserializeObject<HasJoinedResponse>(
                        await joinedResp.Content.ReadAsStringAsync());

                    if (!joinedRespJson.IsValid)
                    {
                        connection.Disconnect("Failed to validate login");
                        return;
                    }

                    userId = new NetUserId(joinedRespJson.UserData!.UserId);
                    userName = joinedRespJson.UserData.UserName;
                    padSuccessMessage = false;
                    type = LoginType.LoggedIn;
                }
                else
                {
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

                    userName = name;

                    (userId, type) = await AssignUserIdAsync(name);
                }

                var endPoint = connection.RemoteEndPoint;
                var connect = await OnConnecting(endPoint, userId, userName, type);
                if (connect.IsDenied)
                {
                    connection.Disconnect($"Connection denied: {connect.DenyReason}");
                    return;
                }

                // Well they're in. Kick a connected client with the same GUID if we have to.
                if (_assignedUserIds.TryGetValue(userId, out var existing))
                {
                    if (_awaitingDisconnectToConnect.Contains(userId))
                    {
                        connection.Disconnect("Stop trying to connect multiple times at once.");
                        return;
                    }

                    _awaitingDisconnectToConnect.Add(userId);
                    try
                    {
                        existing.Disconnect("Another connection has been made with your account.");
                        // Have to wait until they're properly off the server to avoid any collisions.
                        await AwaitDisconnectAsync(existing);
                    }
                    finally
                    {
                        _awaitingDisconnectToConnect.Remove(userId);
                    }
                }

                if (connection.Status == NetConnectionStatus.Disconnecting ||
                    connection.Status == NetConnectionStatus.Disconnected)
                {
                    Logger.InfoS("net",
                        "{ConnectionEndpoint} disconnected during handshake",
                        connection.RemoteEndPoint, userName, userId);

                    return;
                }

                var msg = peer.Peer.CreateMessage();
                var msgResp = new MsgLoginSuccess
                {
                    UserId = userId.UserId,
                    UserName = userName,
                    Type = type
                };
                if (padSuccessMessage)
                {
                    msg.Write(true);
                    msg.WritePadBits();
                }

                msgResp.WriteToBuffer(msg);
                encryption?.Encrypt(msg);
                peer.Peer.SendMessage(msg, connection, NetDeliveryMethod.ReliableOrdered);

                Logger.InfoS("net",
                    "Approved {ConnectionEndpoint} with username {Username} user ID {userId} into the server",
                    connection.RemoteEndPoint, userName, userId);

                // Handshake complete!
                HandleInitialHandshakeComplete(peer, connection, userId, userName, encryption, type);
            }
            catch (ClientDisconnectedException)
            {
                Logger.InfoS("net",
                    $"Peer {NetUtility.ToHexString(connection.RemoteUniqueIdentifier)} disconnected while handshake was in-progress.");
            }
            catch (Exception e)
            {
                connection.Disconnect("Unknown server error occured during handshake.");
                Logger.ErrorS("net", "Exception during handshake with peer {0}:\n{1}",
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

        private sealed class HasJoinedResponse
        {
#pragma warning disable 649
            public bool IsValid;
            public HasJoinedUserData? UserData;

            public sealed class HasJoinedUserData
            {
                public string UserName = default!;
                public Guid UserId = default!;
            }
#pragma warning restore 649
        }
    }
}
