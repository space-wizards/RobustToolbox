using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Reflection;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     A subsystem that acts on all components of a type at once.
    /// </summary>
    /// <remarks>
    ///     This class is instantiated by the <c>EntitySystemManager</c>, and any IoC Dependencies will be resolved.
    /// </remarks>
    [Reflect(false), PublicAPI]
    public abstract partial class EntitySystem : IEntitySystem
    {
        [Dependency] protected readonly IEntityManager EntityManager = default!;
        [Dependency] protected readonly IComponentManager ComponentManager = default!;
        [Dependency] protected readonly IEntitySystemManager EntitySystemManager = default!;

        protected internal List<Type> UpdatesAfter { get; } = new();
        protected internal List<Type> UpdatesBefore { get; } = new();

        IEnumerable<Type> IEntitySystem.UpdatesAfter => UpdatesAfter;
        IEnumerable<Type> IEntitySystem.UpdatesBefore => UpdatesBefore;

        /// <inheritdoc />
        public virtual void Initialize() { }

        /// <inheritdoc />
        public virtual void Update(float frameTime) { }

        /// <inheritdoc />
        public virtual void FrameUpdate(float frameTime) { }

        /// <inheritdoc />
        public virtual void Shutdown()
        {
            ShutdownSubscriptions();
        }


        #region Event Proxy

        protected void RaiseLocalEvent<T>(T message) where T : notnull
        {
            EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
        }

        protected bool RaiseLocalEvent<T>(T message, out T outMessage) where T : notnull
        {
            EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
            outMessage = message;
            return true;
        }

        protected void RaiseLocalEvent(object message)
        {
            EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
        }

        protected bool RaiseLocalEvent(object message, out object outMessage)
        {
            EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
            outMessage = message;
            return true;
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

        protected void RaiseNetworkEvent(EntityEventArgs message, Filter filter)
        {
            foreach (var session in filter.Recipients)
            {
                EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, session.ConnectedClient);
            }
        }

        protected Task<T> AwaitNetworkEvent<T>(CancellationToken cancellationToken)
            where T : EntityEventArgs
        {
            return EntityManager.EventBus.AwaitEvent<T>(EventSource.Network, cancellationToken);
        }

        protected void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = true)
            where TEvent : EntityEventArgs
        {
            EntityManager.EventBus.RaiseLocalEvent(uid, args, broadcast);
        }

        protected bool RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, out TEvent outArgs, bool broadcast = true)
            where TEvent : EntityEventArgs
        {
            EntityManager.EventBus.RaiseLocalEvent(uid, args, broadcast);
            outArgs = args;
            return true;
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
        public static bool TryGet<T>([NotNullWhen(true)] out T? entitySystem) where T : IEntitySystem
        {
            return IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem(out entitySystem);
        }

        #endregion
    }
}
