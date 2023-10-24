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
public abstract partial class SharedPlayerManager
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

    public bool TryGetSessionById(NetUserId user, [NotNullWhen(true)] out ICommonSession? session)
    {
        Lock.EnterReadLock();
        try
        {
            return InternalSessions.TryGetValue(user, out session);
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

    internal CommonSession CreateAndAddSession(NetUserId user, string name)
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

    public virtual void SetAttachedEntity(ICommonSession session, EntityUid? uid)
    {
        if (session.AttachedEntity == uid)
            return;

        ((CommonSession) session).AttachedEntity = uid;
        UpdateState(session);
    }

    public void SetStatus(ICommonSession session, SessionStatus status)
    {
        if (session.Status == status)
            return;

        var old = session.Status;
        ((CommonSession) session).Status = status;

        UpdateState(session);
        PlayerStatusChanged?.Invoke(this, new SessionStatusEventArgs(session, old, status));
    }
}
