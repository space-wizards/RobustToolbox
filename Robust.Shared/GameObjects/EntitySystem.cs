using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Network;
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
    public abstract class EntitySystem : IEntitySystem
    {
        [Dependency] protected readonly IEntityManager EntityManager = default!;
        [Dependency] protected readonly IComponentManager ComponentManager = default!;
        [Dependency] protected readonly IEntitySystemManager EntitySystemManager = default!;

        [Obsolete("You need to create and store the query yourself in a field.")]
        protected IEntityQuery? EntityQuery;

        [Obsolete("You need to use `EntityManager.GetEntities(EntityQuery)`, or store a query yourself.")]
        protected IEnumerable<IEntity> RelevantEntities => EntityQuery != null ? EntityManager.GetEntities(EntityQuery) : EntityManager.GetEntities();

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
        public virtual void Shutdown() { }


        #region Event Proxy

        protected void SubscribeNetworkEvent<T>(EntityEventHandler<T> handler)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeEvent(EventSource.Network, this, handler);
        }

        protected void SubscribeNetworkEvent<T>(EntitySessionEventHandler<T> handler)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeSessionEvent(EventSource.Network, this, handler);
        }

        protected void SubscribeLocalEvent<T>(EntityEventHandler<T> handler)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeEvent(EventSource.Local, this, handler);
        }

        protected void SubscribeLocalEvent<T>(EntitySessionEventHandler<T> handler)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeSessionEvent(EventSource.Local, this, handler);
        }

        protected void UnsubscribeNetworkEvent<T>()
            where T : notnull
        {
            EntityManager.EventBus.UnsubscribeEvent<T>(EventSource.Network, this);
        }

        protected void UnsubscribeLocalEvent<T>()
            where T : notnull
        {
            EntityManager.EventBus.UnsubscribeEvent<T>(EventSource.Local, this);
        }

        protected void RaiseLocalEvent<T>(T message) where T : notnull
        {
            EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
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

        protected Task<T> AwaitNetworkEvent<T>(CancellationToken cancellationToken)
            where T : EntityEventArgs
        {
            return EntityManager.EventBus.AwaitEvent<T>(EventSource.Network, cancellationToken);
        }

        protected void SubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : EntityEventArgs
        {
            EntityManager.EventBus.SubscribeLocalEvent(handler);
        }

        [Obsolete("Use the overload without the handler argument.")]
        protected void UnsubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : EntityEventArgs
        {
            EntityManager.EventBus.UnsubscribeLocalEvent<TComp, TEvent>();
        }

        protected void UnsubscribeLocalEvent<TComp, TEvent>()
            where TComp : IComponent
            where TEvent : EntityEventArgs
        {
            EntityManager.EventBus.UnsubscribeLocalEvent<TComp, TEvent>();
        }

        protected void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = true)
            where TEvent : EntityEventArgs
        {
            EntityManager.EventBus.RaiseLocalEvent(uid, args, broadcast);
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
