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

        public ISawmill Log { get; private set; } = default!;

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

        protected internal List<Type> UpdatesAfter { get; } = new();
        protected internal List<Type> UpdatesBefore { get; } = new();

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
                _replayMan.RecordServerMessage(message);

            foreach (var session in filter.Recipients)
            {
                EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, session.Channel);
            }
        }

        protected void RaiseNetworkEvent(EntityEventArgs message, EntityUid recipient)
        {
            if (_playerMan.TryGetSessionByEntity(recipient, out var session))
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
