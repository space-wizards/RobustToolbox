using System;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    public interface IEntityEventSubscriber { }

    public delegate void EntityEventHandler<in T>(T ev);

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
        public EntitySessionEventArgs(ICommonSession senderSession)
        {
            SenderSession = senderSession;
        }

        public ICommonSession SenderSession { get; }
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
