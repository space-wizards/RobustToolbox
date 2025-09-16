using System;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    public interface IEntityEventSubscriber { }

    public delegate void EntityEventHandler<in T>(T ev);
    public delegate void EntityEventRefHandler<T>(ref T ev);
    public delegate void EntitySessionEventHandler<in T>(T msg, EntitySessionEventArgs args);

    [Serializable, NetSerializable]
    public abstract class EntityEventArgs { }

    [Serializable, NetSerializable]
    public abstract class HandledEntityEventArgs : EntityEventArgs
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }
    }

    [Serializable, NetSerializable]
    public abstract class CancellableEntityEventArgs : EntityEventArgs
    {
        /// <summary>
        ///     Whether this even has been cancelled.
        /// </summary>
        public bool Cancelled { get; private set; }

        /// <summary>
        ///     Cancels the event.
        /// </summary>
        public void Cancel() => Cancelled = true;

        /// <summary>
        ///     Uncancels the event. Don't call this unless you know what you're doing.
        /// </summary>
        public void Uncancel() => Cancelled = false;
    }

    public readonly struct EntitySessionEventArgs
    {
        public EntitySessionEventArgs(ICommonSession senderSession, GameTick lastApplied = default)
        {
            SenderSession = senderSession;
            LastAppliedTick = lastApplied;
        }

        public ICommonSession SenderSession { get; }

        /// <summary>
        /// If this event was sent from a client, this is the tick of the last sever state that the client had applied
        /// at the time that the event was initially raised.
        /// </summary>
        /// <remarks>
        /// This can be used to perform some basic lag compensation.
        /// </remarks>
        public readonly GameTick LastAppliedTick;
    }

    internal readonly struct EntitySessionMessage<T>
    {
        public EntitySessionMessage(EntitySessionEventArgs eventArgs, T message)
        {
            EventArgs = eventArgs;
            Message = message;
        }

        public EntitySessionEventArgs EventArgs { get; }
        public T Message { get; }

        public void Deconstruct(out EntitySessionEventArgs eventArgs, out T message)
        {
            eventArgs = EventArgs;
            message = Message;
        }
    }
}
