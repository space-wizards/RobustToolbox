using System.Runtime.CompilerServices;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Robust.Shared.GameObjects;

public abstract partial class EntManProxy
{
    protected void RaiseLocalEvent<T>(T message) where T : notnull
    {
        EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
    }

    protected void RaiseLocalEvent<T>(ref T message) where T : notnull
    {
        EntityManager.EventBus.RaiseEvent(EventSource.Local, ref message);
    }

    protected void RaiseLocalEvent(object message)
    {
        EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
    }

    protected void QueueLocalEvent(EntityEventArgs message)
    {
        EntityManager.EventBus.QueueEvent(EventSource.Local, message);
    }

    protected void RaiseNetworkEvent(EntityEventArgs message)
    {
        EntityManager.EntityNetManager?.SendSystemNetworkMessage(message);
    }

    protected void RaiseNetworkEvent(EntityEventArgs message, INetChannel channel)
    {
        EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, channel);
    }

    protected void RaiseNetworkEvent(EntityEventArgs message, ICommonSession session)
    {
        EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, session.Channel);
    }

    /// <summary>
    ///     Raises a networked event with some filter.
    /// </summary>
    /// <param name="message">The event to send</param>
    /// <param name="filter">The filter that specifies recipients</param>
    /// <param name="recordReplay">Optional bool specifying whether or not to save this event to replays.</param>
    protected void RaiseNetworkEvent(EntityEventArgs message, Filter filter, bool recordReplay = true)
    {
        if (recordReplay)
            ReplayMan.RecordServerMessage(message);

        foreach (var session in filter.Recipients)
        {
            EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, session.Channel);
        }
    }

    protected void RaiseNetworkEvent(EntityEventArgs message, EntityUid recipient)
    {
        if (PlayerManager.TryGetSessionByEntity(recipient, out var session))
            EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, session.Channel);
    }

    protected void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = false)
        where TEvent : notnull
    {
        EntityManager.EventBus.RaiseLocalEvent(uid, args, broadcast);
    }

    protected void RaiseLocalEvent(EntityUid uid, object args, bool broadcast = false)
    {
        EntityManager.EventBus.RaiseLocalEvent(uid, args, broadcast);
    }

    protected void RaiseLocalEvent<TEvent>(EntityUid uid, ref TEvent args, bool broadcast = false)
        where TEvent : notnull
    {
        EntityManager.EventBus.RaiseLocalEvent(uid, ref args, broadcast);
    }

    protected void RaiseLocalEvent(EntityUid uid, ref object args, bool broadcast = false)
    {
        EntityManager.EventBus.RaiseLocalEvent(uid, ref args, broadcast);
    }

    /// <summary>
    ///     Sends a networked message to the server, while also repeatedly raising it locally for every time this tick gets re-predicted.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RaisePredictiveEvent<T>(T msg) where T : EntityEventArgs
    {
        EntityManager.RaisePredictiveEvent(msg);
    }
}
