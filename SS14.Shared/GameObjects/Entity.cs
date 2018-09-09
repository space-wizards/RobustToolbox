using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Serialization;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace SS14.Shared.GameObjects
{
    /// <inheritdoc />
    public sealed class Entity : IEntity
    {
        #region Members

        private string _name;

        /// <inheritdoc />
        public IEntityManager EntityManager { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public EntityUid Uid { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public EntityPrototype Prototype { get; internal set; }

        /// <inheritdoc />
        [ViewVariables]
        public string Description
        {
            get
            {
                if (_description == null)
                    return Prototype.Description;
                return _description;
            }
            set => _description = value;
        }

        /// <summary>
        /// Private value which can override the prototype value for the description
        /// </summary>
        private string _description;

        /// <inheritdoc />
        [ViewVariables]
        public uint LastModifiedTick { get; private set; }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                Dirty();
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public bool Initialized { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public bool Deleted { get; private set; }

        private ITransformComponent _transform;
        /// <inheritdoc />
        [ViewVariables]
        public ITransformComponent Transform => _transform ?? (_transform = GetComponent<ITransformComponent>());

        #endregion Members

        #region Initialization

        /// <summary>
        ///     Sets fundamental managers after the entity has been created.
        /// </summary>
        /// <remarks>
        ///     This is a separate method because C# makes constructors painful.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the method is called and the entity already has initialized managers.
        /// </exception>
        public void SetManagers(IEntityManager entityManager)
        {
            if (EntityManager != null)
            {
                throw new InvalidOperationException("Entity already has initialized managers.");
            }

            EntityManager = entityManager;
        }

        /// <inheritdoc />
        public bool IsValid()
        {
            return !Deleted;
        }

        /// <summary>
        ///     Initialize the entity's UID. This can only be called once.
        /// </summary>
        /// <param name="uid">The new UID.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the method is called and the entity already has a UID.
        /// </exception>
        public void SetUid(EntityUid uid)
        {
            if (!uid.IsValid())
                throw new ArgumentException("Uid is not valid.", nameof(uid));

            if (Uid.IsValid())
                throw new InvalidOperationException("Entity already has a UID.");

            Uid = uid;
        }

        /// <summary>
        ///     Calls Initialize() on all registered components.
        /// </summary>
        public void InitializeComponents()
        {
            // Initialize() can modify the collection of components.
            var components = EntityManager.ComponentManager.GetComponents(Uid).ToList();
            for (int i = 0; i < components.Count; i++)
            {
                var comp = (Component)components[i];
                if (comp != null && !comp.Initialized)
                    comp.Initialize();
            }
        }

        /// <summary>
        ///     Calls Startup() on all registered components.
        /// </summary>
        public void StartAllComponents()
        {
            // Startup() can modify _components
            // TODO: This code can only handle additions to the list. Is there a better way?
            var components = EntityManager.ComponentManager.GetComponents(Uid).ToList();
            for (int i = 0; i < components.Count; i++)
            {
                var comp = (Component)components[i];
                if (comp != null && comp.Initialized && !comp.Running && !comp.Deleted)
                    comp.Startup();
            }
        }

        /// <summary>
        ///     Sets up the entity into a valid initial state.
        /// </summary>
        public void Initialize()
        {
            Initialized = true;
        }

        #endregion Initialization

        #region Unified Messaging

        /// <inheritdoc />
        public void SendMessage(IComponent owner, ComponentMessage message)
        {
            var components = EntityManager.ComponentManager.GetComponents(Uid);
            foreach (var component in components)
            {
                if (owner != component)
                    component.HandleMessage(message, null, owner);
            }
        }

        /// <inheritdoc />
        public void SendNetworkMessage(IComponent owner, ComponentMessage message, INetChannel channel = null)
        {
            if (message.Directed)
            {
                EntityManager.EntityNetManager.SendDirectedComponentNetworkMessage(channel, this, owner, message);
            }
            else
            {
                EntityManager.EntityNetManager.SendComponentNetworkMessage(this, owner, message);
            }
        }

        #endregion Unified Messaging

        #region Network messaging

        /// <inheritdoc />
        public void HandleNetworkMessage(IncomingEntityMessage message)
        {
            switch (message.Message.Type)
            {
                case EntityMessageType.ComponentMessage:
                    {
                        var compMsg = message.Message.ComponentMessage;
                        var compChannel = message.Message.MsgChannel;
                        compMsg.Remote = true;

                        if (compMsg.Directed)
                        {
                            if (EntityManager.ComponentManager.TryGetComponent(Uid, message.Message.NetId, out var component))
                                component.HandleMessage(compMsg, compChannel);
                        }
                        else
                        {
                            foreach (var component in EntityManager.ComponentManager.GetComponents(Uid))
                            {
                                component.HandleMessage(compMsg, compChannel);
                            }
                        }
                    }
                    break;
            }
        }

        #endregion Network messaging

        #region IEntity Members

        #region Component Events

        /// <inheritdoc />
        public void SubscribeEvent<T>(EntityEventHandler<EntityEventArgs> evh, IEntityEventSubscriber s)
            where T : EntityEventArgs
        {
            EntityManager.SubscribeEvent<T>(evh, s);
        }

        /// <inheritdoc />
        public void UnsubscribeEvent<T>(IEntityEventSubscriber s)
            where T : EntityEventArgs
        {
            EntityManager.UnsubscribeEvent<T>(s);
        }

        /// <inheritdoc />
        public void RaiseEvent(EntityEventArgs toRaise)
        {
            EntityManager.RaiseEvent(this, toRaise);
        }

        #endregion Component Events

        #endregion IEntity Members

        #region Entity Systems

        /// <inheritdoc />
        public bool Match(IEntityQuery query)
        {
            return query.Match(this);
        }

        #endregion Entity Systems

        #region Components

        /// <summary>
        ///     Public method to add a component to an entity.
        ///     Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddComponent(Component component)
        {
            EntityManager.ComponentManager.AddComponent(this, component);
        }

        /// <inheritdoc />
        public T AddComponent<T>()
            where T : Component, new()
        {
            return EntityManager.ComponentManager.AddComponent<T>(this);
        }

        private void AddComponent(Component component, bool overwrite)
        {
            EntityManager.ComponentManager.AddComponent(this, component, overwrite);
        }

        /// <inheritdoc />
        public void RemoveComponent<T>()
        {
            EntityManager.ComponentManager.RemoveComponent<T>(Uid);
        }

        private void RemoveComponent(IComponent component)
        {
            EntityManager.ComponentManager.RemoveComponent(Uid, component);
        }

        /// <inheritdoc />
        public bool HasComponent<T>()
        {
            return EntityManager.ComponentManager.HasComponent(Uid, typeof(T));
        }

        /// <inheritdoc />
        public bool HasComponent(Type type)
        {
            return EntityManager.ComponentManager.HasComponent(Uid, type);
        }

        private bool HasComponent(uint netId)
        {
            return EntityManager.ComponentManager.HasComponent(Uid, netId);
        }

        /// <inheritdoc />
        public T GetComponent<T>()
        {
            return (T)EntityManager.ComponentManager.GetComponent(Uid, typeof(T));
        }

        /// <inheritdoc />
        public IComponent GetComponent(Type type)
        {
            return EntityManager.ComponentManager.GetComponent(Uid, type);
        }

        /// <inheritdoc />
        public IComponent GetComponent(uint netID)
        {
            return EntityManager.ComponentManager.GetComponent(Uid, netID);
        }

        /// <inheritdoc />
        public bool TryGetComponent<T>(out T component)
            where T : class
        {
            return EntityManager.ComponentManager.TryGetComponent(Uid, out component);
        }

        /// <inheritdoc />
        public bool TryGetComponent(Type type, out IComponent component)
        {
            return EntityManager.ComponentManager.TryGetComponent(Uid, type, out component);
        }

        /// <inheritdoc />
        public bool TryGetComponent(uint netID, out IComponent component)
        {
            return EntityManager.ComponentManager.TryGetComponent(Uid, netID, out component);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            EntityManager.ComponentManager.RemoveComponents(Uid);

            // Entity manager culls us because we're set to Deleted.
            Deleted = true;
        }

        /// <inheritdoc />
        public void Delete()
        {
            EntityManager.DeleteEntity(this);
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetAllComponents()
        {
            return EntityManager.ComponentManager.GetComponents(Uid).Where(comp => !comp.Deleted);
        }

        /// <inheritdoc />
        public IEnumerable<T> GetAllComponents<T>()
        {
            return GetAllComponents().OfType<T>();
        }

        #endregion Components

        #region GameState

        /// <inheritdoc />
        public void HandleEntityState(EntityState state)
        {
            Name = state.StateData.Name;
            var synchedComponentTypes = state.StateData.SynchedComponentTypes;
            foreach (var t in synchedComponentTypes)
            {
                if (HasComponent(t.Item1) && GetComponent(t.Item1).Name != t.Item2)
                    RemoveComponent(GetComponent(t.Item1));

                if (!HasComponent(t.Item1))
                    AddComponent((Component)IoCManager.Resolve<IComponentFactory>().GetComponent(t.Item2), true);
            }

            foreach (var compState in state.ComponentStates)
            {
                compState.ReceivedTime = state.ReceivedTime;

                if (!TryGetComponent(compState.NetID, out var component))
                    continue;

                if (compState.GetType() != component.StateType)
                    throw new InvalidOperationException($"Incorrect component state type: {component.StateType}, component: {component.GetType()}");

                component.HandleComponentState(compState);
            }
        }

        /// <inheritdoc />
        public EntityState GetEntityState(uint fromTick)
        {
            var compStates = GetComponentStates(fromTick);

            var synchedComponentTypes = EntityManager.ComponentManager.GetNetComponents(Uid)
                .Select(t => new Tuple<uint, string>(t.NetID.Value, t.Name))
                .ToList();

            var es = new EntityState(
                Uid,
                compStates,
                Prototype.ID,
                Name,
                synchedComponentTypes);
            return es;
        }

        /// <summary>
        ///     Server-side method to get the state of all our components
        /// </summary>
        /// <returns></returns>
        private List<ComponentState> GetComponentStates(uint fromTick)
        {
            return GetAllComponents()
                .Where(c => c.NetID != null && c.NetSyncEnabled && c.LastModifiedTick >= fromTick)
                .Select(component => component.GetComponentState())
                .ToList();
        }

        public void Dirty()
        {
            LastModifiedTick = EntityManager.CurrentTick;
        }

        #endregion GameState

        public override string ToString()
        {
            return $"{Name} ({Uid}, {Prototype.ID})";
        }
    }
}
