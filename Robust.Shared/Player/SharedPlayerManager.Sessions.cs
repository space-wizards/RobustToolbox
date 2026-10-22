using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Player;

// This partial class contains code related to getting player sessions via their user ids and names
internal abstract partial class SharedPlayerManager
{
    protected readonly ReaderWriterLockSlim Lock = new();

    [ViewVariables]
    protected readonly Dictionary<NetUserId, ICommonSession> InternalSessions = new();

    public IReadOnlyDictionary<NetUserId, ICommonSession> SessionsDict
    {
        get
        {
            Lock.EnterReadLock();
            try
            {
                return InternalSessions.ShallowClone();
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }
    }

    public ICommonSession[] Sessions
    {
        get
        {
            Lock.EnterReadLock();
            try
            {
                return InternalSessions.Values.ToArray();
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }
    }

    public bool TryGetSessionById([NotNullWhen(true)] NetUserId? user, [NotNullWhen(true)] out ICommonSession? session)
    {
        if (user == null)
        {
            session = null;
            return false;
        }

        Lock.EnterReadLock();
        try
        {
            return InternalSessions.TryGetValue(user.Value, out session);
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }

    public virtual ICommonSession[] NetworkedSessions => Sessions;

    public bool TryGetSessionByUsername(string username, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;
        return UserIdMap.TryGetValue(username, out var userId) && TryGetSessionById(userId, out session);
    }

    public ICommonSession GetSessionByChannel(INetChannel channel)
        => GetSessionById(channel.UserId);

    public bool TryGetSessionByChannel(INetChannel channel, [NotNullWhen(true)] out ICommonSession? session)
        => TryGetSessionById(channel.UserId, out session);

    public ICommonSession GetSessionById(NetUserId user)
    {
        if (!TryGetSessionById(user, out var session))
            throw new KeyNotFoundException();
        return session;
    }

    public bool ValidSessionId(NetUserId user) => TryGetSessionById(user, out _);

    public abstract bool TryGetSessionByEntity(EntityUid uid, [NotNullWhen(true)] out ICommonSession? session);

    protected virtual CommonSession CreateSession(NetUserId user, string name, SessionData data)
    {
        return new CommonSession(user, name, data);
    }

    public ICommonSession CreateAndAddSession(INetChannel channel)
    {
        var session = (ICommonSessionInternal)CreateAndAddSession(channel.UserId, channel.UserName);
        session.SetChannel(channel);
        return session;
    }

    public ICommonSession CreateAndAddSession(NetUserId user, string name)
    {
        Lock.EnterWriteLock();
        CommonSession session;
        try
        {
            UserIdMap[name] = user;
            if (!PlayerData.TryGetValue(user, out var data))
                PlayerData[user] = data = new(user, name);

            session = CreateSession(user, name, data);
            InternalSessions.Add(user, session);
        }
        finally
        {
            Lock.ExitWriteLock();
        }

        UpdateState(session);

        return session;
    }

    public void RemoveSession(ICommonSession session, bool removeData = false)
        => RemoveSession(session.UserId, removeData);

    public void RemoveSession(NetUserId user, bool removeData = false)
    {
        Lock.EnterWriteLock();
        try
        {
            InternalSessions.Remove(user);
            if (removeData)
                PlayerData.Remove(user);
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    /// <inheritdoc cref="ISharedPlayerManager.SetAttachedEntity"/>
    public virtual bool SetAttachedEntity(
        [NotNullWhen(true)] ICommonSession? session,
        EntityUid? uid,
        out ICommonSession? kicked,
        bool force = false)
    {
        kicked = null;
        if (session == null)
            return false;

        if (session.AttachedEntity == uid)
        {
            DebugTools.Assert(uid == null || EntManager.HasComponent<ActorComponent>(uid));
            return true;
        }

        if (uid != null)
            return Attach(session, uid.Value, out kicked, force);

        Detach(session);
        return true;
    }

    private void Detach(ICommonSession session)
    {
        if (session.AttachedEntity is not {} uid)
            return;

        ((ICommonSessionInternal) session).SetAttachedEntity(null);
        UpdateState(session);

        if (EntManager.TryGetComponent(uid, out ActorComponent? actor) && actor.LifeStage <= ComponentLifeStage.Running)
        {
            actor.PlayerSession = default!;
            EntManager.RemoveComponent(uid, actor);
        }

        EntManager.EventBus.RaiseLocalEvent(uid, new PlayerDetachedEvent(uid, session), true);
    }

    private bool Attach(ICommonSession session, EntityUid uid, out ICommonSession? kicked, bool force = false)
    {
        kicked = null;
        if (!EntManager.TryGetComponent(uid, out MetaDataComponent? meta))
            return false;

        if (meta.EntityLifeStage >= EntityLifeStage.Terminating)
            return false;

        if (EntManager.EnsureComponent<ActorComponent>(uid, out var actor))
        {
            // component already existed.
            if (!force)
                return false;

            kicked = actor.PlayerSession;

            if (kicked != null)
                Detach(kicked);
        }

        if (_netMan.IsServer)
            EntManager.EnsureComponent<EyeComponent>(uid);

        if (session.AttachedEntity != null)
            Detach(session);

        ((ICommonSessionInternal) session).SetAttachedEntity(uid);
        actor.PlayerSession = session;
        UpdateState(session);
        EntManager.EventBus.RaiseLocalEvent(uid, new PlayerAttachedEvent(uid, session), true);
        return true;
    }

    public void SetStatus(ICommonSession session, SessionStatus status)
    {
        if (session.Status == status)
            return;

        var old = session.Status;
        ((ICommonSessionInternal) session).SetStatus(status);

        UpdateState(session);
        PlayerStatusChanged?.Invoke(this, new SessionStatusEventArgs(session, old, status));
    }

    public void SetPing(ICommonSession session, short ping)
    {
        ((ICommonSessionInternal) session).SetPing(ping);
        UpdateState(session);
    }

    public void SetName(ICommonSession session, string name)
    {
        ((ICommonSessionInternal) session).SetName(name);
        UpdateState(session);
    }

    public void JoinGame(ICommonSession session)
    {
        // This currently just directly sets the session's status, as this was the old behaviour.
        // In future, this should probably check if the session is currently in a valid state.
        SetStatus(session, SessionStatus.InGame);
    }
}
