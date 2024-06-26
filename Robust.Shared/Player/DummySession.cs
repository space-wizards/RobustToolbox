using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Robust.Shared.Player;

/// <summary>
/// This is a mock session for use with integration tests and benchmarks. It uses a <see cref="DummyChannel"/> as
/// its <see cref="INetChannel"/>, which doesn't support actually sending any messages.
/// </summary>
internal sealed class DummySession : ICommonSessionInternal
{
    public EntityUid? AttachedEntity {get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Connecting;
    public NetUserId UserId => UserData.UserId;
    public string Name  => UserData.UserName;

    public short Ping { get; set; }

    public INetChannel Channel
    {
        get => DummyChannel;
        [Obsolete]
        set => throw new NotSupportedException();
    }

    public LoginType AuthType { get; set; } = LoginType.GuestAssigned;
    public HashSet<EntityUid> ViewSubscriptions { get; } = new();
    public DateTime ConnectedTime { get; set; }
    public SessionState State { get; set; } = new();
    public SessionData Data { get; set; }
    public bool ClientSide { get; set; }
    public NetUserData UserData { get; set; }

    public DummyChannel DummyChannel;

    public DummySession(NetUserId userId, string userName, SessionData data)
    {
        Data = data;
        UserData = new(userId, userName)
        {
            HWId = ImmutableArray<byte>.Empty
        };
        DummyChannel = new(this);
    }

    public void SetStatus(SessionStatus status)
    {
        Status = status;
    }

    public void SetAttachedEntity(EntityUid? uid)
    {
        AttachedEntity = uid;
    }

    public void SetPing(short ping)
    {
        Ping = ping;
    }

    public void SetName(string name)
    {
        UserData = new(UserData.UserId, name)
        {
            HWId = UserData.HWId
        };
    }

    public void SetChannel(INetChannel channel)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// A mock NetChannel for use in integration tests and benchmarks.
/// </summary>
internal sealed class DummyChannel(DummySession session) : INetChannel
{
    public readonly DummySession Session = session;
    public NetUserData UserData => Session.UserData;
    public short Ping => Session.Ping;
    public string UserName => Session.Name;
    public LoginType AuthType => Session.AuthType;
    public NetUserId UserId => Session.UserId;

    public int CurrentMtu { get; set; } = default;
    public long ConnectionId { get; set; } = default;
    public TimeSpan RemoteTimeOffset { get; set; } = default;
    public TimeSpan RemoteTime  { get; set; } = default;
    public bool IsConnected { get; set; } = true;
    public bool IsHandshakeComplete { get; set; } = true;

    // This is just pilfered from IntegrationNetChannel
    public IPEndPoint RemoteEndPoint { get; } = new(IPAddress.Loopback, 1212);

    // Only used on server, contains the encryption to use for this channel.
    public NetEncryption? Encryption { get; set; }

    public INetManager NetPeer => throw new NotImplementedException();

    public T CreateNetMessage<T>() where T : NetMessage, new()
    {
        throw new NotImplementedException();
    }

    public void SendMessage(NetMessage message)
    {
        throw new NotImplementedException();
    }

    public void Disconnect(string reason)
    {
        throw new NotImplementedException();
    }

    public void Disconnect(string reason, bool sendBye)
    {
        throw new NotImplementedException();
    }
}
