using System;
using System.Reflection.Metadata;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    public interface IEntityEventSubscriber
    {
    }

    public delegate void EntityEventHandler<in T>(T ev);

    public delegate void EntityEventRefHandler<T>(ref T ev);

    public delegate void EntitySessionEventHandler<in T>(T msg, EntitySessionEventArgs args);

    [Serializable, NetSerializable]
    public abstract class EntityEventArgs
    {
    }

    [Obsolete]
    [Serializable, NetSerializable]
    public abstract class HandledEntityEventArgs : EntityEventArgs
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }
    }

    [Obsolete]
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

    /// <summary>
    /// An interface which allows an event to be "Handled" meaning a method has successfully responded to it in some way.
    /// </summary>
    public interface IHandleableEvent
    {
        bool Handled { get; protected set; }

        /// <summary>
        /// Handles the event, call this when your method has correctly responded to the event implementing this interface.
        /// </summary>
        public void Handle()
        {
            Handled = true;
        }
    }

    /// <summary>
    /// An interface which allows an event to be "Canceled" meaning a method has negatively responded to it in some way.
    /// This is typically used for attempt events to prevent further code from being run.
    /// </summary>
    public interface ICancelableEvent
    {
        bool Cancelled { get; protected set; }

        /// <summary>
        /// Cancels the event, call this when your method wishes to cancel the event implementing this interface.
        /// </summary>
        public void Cancel()
        {
            Cancelled = true;
        }
    }

    /// <summary>
    /// Extension methods for Events so that inheritors of these event interfaces don't need to redefine their public methods.
    /// These are filtered by struct and interfaces respectively because you should really be using ByRef structs for your events.
    /// </summary>
    public static class EventExtensions
    {
        extension<T>(T ev) where T : struct, IHandleableEvent
        {
            public void Handle() => ev.Handle();
        }

        extension<T>(T ev) where T : struct, IHandleableEvent
        {
            public void Cancel() => ev.Cancel();
        }
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
