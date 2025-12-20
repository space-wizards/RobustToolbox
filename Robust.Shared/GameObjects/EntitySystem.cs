using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Replays;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     A subsystem that acts on all components of a type at once.
    /// </summary>
    /// <remarks>
    ///     This class is instantiated by the <c>EntitySystemManager</c>, and any IoC Dependencies will be resolved.
    /// </remarks>
    [Reflect(false), PublicAPI]
    public abstract partial class EntitySystem : IEntitySystem, IPostInjectInit
    {
        [Dependency] protected readonly EntityManager EntityManager = default!;
        [Dependency] protected readonly ILogManager LogManager = default!;
        [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
        [Dependency] private readonly IReplayRecordingManager _replayMan = default!;
        [Dependency] protected readonly ILocalizationManager Loc = default!;

        protected IComponentFactory Factory => EntityManager.ComponentFactory;

        /// <summary>
        ///     A logger sawmill for logging debug/informational messages into.
        /// </summary>
        public ISawmill Log { get; private set; } = default!;

        /// <summary>
        ///     The name for the preprovided log sawmill <see cref="Log"/>.
        /// </summary>
        protected virtual string SawmillName
        {
            get
            {
                var name = GetType().Name;

                // Strip trailing "system"
                if (name.EndsWith("System"))
                    name = name.Substring(0, name.Length - "System".Length);

                // Convert CamelCase to snake_case
                // Ignore if all uppercase, assume acronym (e.g. NPC or HTN)
                if (name.All(char.IsUpper))
                {
                    name = name.ToLower(CultureInfo.InvariantCulture);
                }
                else
                {
                    name = string.Concat(name.Select(x => char.IsUpper(x) ? $"_{char.ToLower(x)}" : x.ToString()));
                    name = name.Trim('_');
                }

                return $"system.{name}";
            }
        }

        /// <summary>
        ///     A list of systems this system MUST update after. This determines the overall update order for systems.
        /// </summary>
        protected internal List<Type> UpdatesAfter { get; } = new();
        /// <summary>
        ///     A list of systems this system MUST update before. This determines the overall update order for systems.
        /// </summary>
        protected internal List<Type> UpdatesBefore { get; } = new();

        /// <summary>
        ///     Whether this system will also tick outside of predicted ticks on the client.
        /// </summary>
        /// <seealso cref="Update"/>
        public bool UpdatesOutsidePrediction { get; protected internal set; }

        IEnumerable<Type> IEntitySystem.UpdatesAfter => UpdatesAfter;
        IEnumerable<Type> IEntitySystem.UpdatesBefore => UpdatesBefore;

        protected EntitySystem()
        {
            Subs = new Subscriptions(this);
        }

        /// <inheritdoc />
        [MustCallBase(true)]
        public virtual void Initialize() { }

        /// <inheritdoc />
        /// <remarks>
        /// Not ran on the client if prediction is disabled and
        /// <see cref="UpdatesOutsidePrediction"/> is false (the default).
        /// </remarks>
        [MustCallBase(true)]
        public virtual void Update(float frameTime) { }

        /// <inheritdoc />
        [MustCallBase(true)]
        public virtual void FrameUpdate(float frameTime) { }

        /// <inheritdoc />
        [MustCallBase(true)]
        public virtual void Shutdown()
        {
            ShutdownSubscriptions();
        }

        #region Event Proxy

        /// <summary>
        ///     Raise an event on the event bus, broadcasted locally to all listeners by value.
        /// </summary>
        /// <param name="message">The message to send, consuming it.</param>
        /// <typeparam name="T">The type of the message to send.</typeparam>
        protected void RaiseLocalEvent<T>(T message) where T : notnull
        {
            EntityManager.EventBusInternal.RaiseEvent(EventSource.Local, message);
        }

        /// <summary>
        ///     Raise an event on the event bus, broadcasted locally to all listeners by ref.
        /// </summary>
        /// <param name="message">The location of a message, to be sent by reference and modified in place.</param>
        /// <typeparam name="T">The type of the message to send.</typeparam>
        protected void RaiseLocalEvent<T>(ref T message) where T : notnull
        {
            EntityManager.EventBusInternal.RaiseEvent(EventSource.Local, ref message);
        }

        /// <summary>
        ///     Raise an event of unknown type on the event bus, broadcasted locally to all listeners of its underlying
        ///     type.
        /// </summary>
        /// <param name="message">The message to send.</param>
        protected void RaiseLocalEvent(object message)
        {
            EntityManager.EventBusInternal.RaiseEvent(EventSource.Local, message);
        }

        /// <summary>
        ///     Queue an event to broadcast locally at the end of the tick.
        /// </summary>
        /// <param name="message">The entity event to raise.</param>
        protected void QueueLocalEvent(EntityEventArgs message)
        {
            EntityManager.EventBusInternal.QueueEvent(EventSource.Local, message);
        }

        /// <summary>
        ///     Queue a networked event to be broadcast to all clients at the end of the tick.
        /// </summary>
        /// <param name="message">The entity event to raise.</param>
        protected void RaiseNetworkEvent(EntityEventArgs message)
        {
            EntityManager.EntityNetManager?.SendSystemNetworkMessage(message);
        }

        /// <summary>
        ///     Queue a networked event to be sent to a specific connection at the end of the tick.
        /// </summary>
        /// <param name="message">The entity event to raise.</param>
        /// <param name="channel">The session to send the event to.</param>
        protected void RaiseNetworkEvent(EntityEventArgs message, INetChannel channel)
        {
            EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, channel);
        }

        /// <summary>
        ///     Queue a networked event to be sent to a specific session at the end of the tick.
        /// </summary>
        /// <param name="message">The entity event to raise.</param>
        /// <param name="session">The session to send the event to.</param>
        protected void RaiseNetworkEvent(EntityEventArgs message, ICommonSession session)
        {
            EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, session.Channel);
        }

        /// <summary>
        ///     Raises a networked event with some filter.
        /// </summary>
        /// <param name="message">The entity event to raise.</param>
        /// <param name="filter">The filter that specifies recipients</param>
        /// <param name="recordReplay">Optional bool specifying whether or not to save this event to replays.</param>
        protected void RaiseNetworkEvent(EntityEventArgs message, Filter filter, bool recordReplay = true)
        {
            if (recordReplay)
                _replayMan.RecordServerMessage(message);

            foreach (var session in filter.Recipients)
            {
                EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, session.Channel);
            }
        }

        /// <summary>
        ///     Raise a networked event for a given recipient entity, if there's a client attached.
        /// </summary>
        /// <param name="message">The entity event to raise.</param>
        /// <param name="recipient">The entity to look up a session to send to on.</param>
        protected void RaiseNetworkEvent(EntityEventArgs message, EntityUid recipient)
        {
            if (_playerMan.TryGetSessionByEntity(recipient, out var session))
                EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, session.Channel);
        }

        /// <summary>
        ///     Raise a local event, optionally broadcasted, on a specific entity by value.
        /// </summary>
        /// <param name="uid">The entity to raise the event on.</param>
        /// <param name="args">The event to raise, sent by value.</param>
        /// <param name="broadcast">Whether to broadcast the event alongside raising it directed.</param>
        /// <typeparam name="TEvent">The type of the event to raise.</typeparam>
        protected void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = false)
            where TEvent : notnull
        {
            EntityManager.EventBusInternal.RaiseLocalEvent(uid, args, broadcast);
        }

        /// <summary>
        ///     Raise a local event, optionally broadcasted, on a specific entity by value.
        ///     This raise the event for <paramref name="args"/>' underlying concrete type.
        /// </summary>
        /// <param name="uid">The entity to raise the event on.</param>
        /// <param name="args">The event to raise, sent by value.</param>
        /// <param name="broadcast">Whether to broadcast the event alongside raising it directed.</param>
        protected void RaiseLocalEvent(EntityUid uid, object args, bool broadcast = false)
        {
            EntityManager.EventBusInternal.RaiseLocalEvent(uid, args, broadcast);
        }

        /// <summary>
        ///     Raise a local event, optionally broadcasted, on a specific entity by ref.
        /// </summary>
        /// <param name="uid">The entity to raise the event on.</param>
        /// <param name="args">The event to raise, sent by ref.</param>
        /// <param name="broadcast">Whether to broadcast the event alongside raising it directed.</param>
        /// <typeparam name="TEvent">The type of the event to raise.</typeparam>
        protected void RaiseLocalEvent<TEvent>(EntityUid uid, ref TEvent args, bool broadcast = false)
            where TEvent : notnull
        {
            EntityManager.EventBusInternal.RaiseLocalEvent(uid, ref args, broadcast);
        }

        /// <summary>
        ///     Raise a local event, optionally broadcasted, on a specific entity by ref.
        ///     This raise the event for <paramref name="args"/>' underlying concrete type.
        /// </summary>
        /// <param name="uid">The entity to raise the event on.</param>
        /// <param name="args">The event to raise, sent by ref.</param>
        /// <param name="broadcast">Whether to broadcast the event alongside raising it directed.</param>
        protected void RaiseLocalEvent(EntityUid uid, ref object args, bool broadcast = false)
        {
            EntityManager.EventBusInternal.RaiseLocalEvent(uid, ref args, broadcast);
        }

        /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityEventBus.RaiseComponentEvent``2(Robust.Shared.GameObjects.EntityUid,``1,``0@)"/>
        protected void RaiseComponentEvent<TEvent, TComp>(EntityUid uid, TComp comp, ref TEvent args)
            where TEvent : notnull
            where TComp : IComponent
        {
            EntityManager.EventBusInternal.RaiseComponentEvent(uid, comp, ref args);
        }

        /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityEventBus.RaiseComponentEvent``2(Robust.Shared.GameObjects.EntityUid,``1,``0@)"/>
        public void RaiseComponentEvent<TEvent>(EntityUid uid, IComponent component, ref TEvent args)
            where TEvent : notnull
        {
            EntityManager.EventBusInternal.RaiseComponentEvent(uid, component, ref args);
        }

        #endregion

        #region Static Helpers
        /*
         NOTE: Static helpers relating to EntitySystems are here rather than in a
         static helper class for conciseness / usability. If we had an "EntitySystems" static class
         it would conflict with any imported namespace called "EntitySystems" and require using alias directive, and
         if we called it something longer like "EntitySystemUtility", writing out "EntitySystemUtility.Get" seems
         pretty tedious for a potentially commonly-used method. Putting it here allows writing "EntitySystem.Get"
         which is nice and concise.
         */

        /// <summary>
        /// Gets the indicated entity system.
        /// </summary>
        /// <typeparam name="T">entity system to get</typeparam>
        /// <returns></returns>
        [Obsolete("Either use a dependency, resolve and cache IEntityManager manually, or use EntityManager.System<T>()")]
        public static T Get<T>() where T : IEntitySystem
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<T>();
        }

        /// <summary>
        /// Tries to get an entity system of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of entity system to find.</typeparam>
        /// <param name="entitySystem">instance matching the specified type (if exists).</param>
        /// <returns>If an instance of the specified entity system type exists.</returns>
        [Obsolete("Either use a dependency, resolve and cache IEntityManager manually, or use EntityManager.System<T>()")]
        public static bool TryGet<T>([NotNullWhen(true)] out T? entitySystem) where T : IEntitySystem
        {
            return IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem(out entitySystem);
        }

        #endregion


        void IPostInjectInit.PostInject() => PostInject();

        protected virtual void PostInject()
        {
            Log = LogManager.GetSawmill(SawmillName);

#if !DEBUG
            Log.Level = LogLevel.Info;
#endif
        }
    }
}
