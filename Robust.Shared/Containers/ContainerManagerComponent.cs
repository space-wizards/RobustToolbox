using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Holds data about a set of entity containers on this entity.
    /// </summary>
    [ComponentReference(typeof(IContainerManager))]
    [NetworkedComponent]
    [ComponentProtoName("ContainerContainer")]
    public sealed class ContainerManagerComponent : Component, IContainerManager, ISerializationHooks
    {
        [Dependency] private readonly IDynamicTypeFactoryInternal _dynFactory = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly INetManager _netMan = default!;

        [DataField("containers")]
        public Dictionary<string, IContainer> Containers = new();

        void ISerializationHooks.AfterDeserialization()
        {
            // TODO remove ISerializationHooks I guess the IDs can be set by a custom serializer for the dictionary? But
            // the component??? Maybe other systems need to stop assuming that containers have been initialized during
            // their own init.
            foreach (var (id, container) in Containers)
            {
                var baseContainer = (BaseContainer) container;
                baseContainer.Manager = this;
                baseContainer.ID = id;
            }
        }

        /// <inheritdoc />
        protected override void OnRemove()
        {
            base.OnRemove();

            foreach (var container in Containers.Values)
            {
                container.Shutdown(_entMan, _netMan);
            }

            Containers.Clear();
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            // naive implementation that just sends the full state of the component
            Dictionary<string, ContainerManagerComponentState.ContainerData> containerSet = new(Containers.Count);

            foreach (var container in Containers.Values)
            {
                var uidArr = new EntityUid[container.ContainedEntities.Count];

                for (var index = 0; index < container.ContainedEntities.Count; index++)
                {
                    uidArr[index] = container.ContainedEntities[index];
                }

                var sContainer = new ContainerManagerComponentState.ContainerData(container.ContainerType, container.ID, container.ShowContents, container.OccludesLight, uidArr);
                containerSet.Add(container.ID, sContainer);
            }

            return new ContainerManagerComponentState(containerSet);
        }

        /// <inheritdoc />
        public T MakeContainer<T>(string id)
            where T : IContainer
        {
            return (T) MakeContainer(id, typeof(T));
        }

        /// <inheritdoc />
        public IContainer GetContainer(string id)
        {
            return Containers[id];
        }

        /// <inheritdoc />
        public bool HasContainer(string id)
        {
            return Containers.ContainsKey(id);
        }

        /// <inheritdoc />
        public bool TryGetContainer(string id, [NotNullWhen(true)] out IContainer? container)
        {
            var ret = Containers.TryGetValue(id, out var cont);
            container = cont!;
            return ret;
        }

        /// <inheritdoc />
        public bool TryGetContainer(EntityUid entity, [NotNullWhen(true)] out IContainer? container)
        {
            foreach (var contain in Containers.Values)
            {
                if (!contain.Deleted && contain.Contains(entity))
                {
                    container = contain;
                    return true;
                }
            }

            container = default;
            return false;
        }

        /// <inheritdoc />
        public bool ContainsEntity(EntityUid entity)
        {
            foreach (var container in Containers.Values)
            {
                if (!container.Deleted && container.Contains(entity)) return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool Remove(EntityUid toremove,
            TransformComponent? xform = null,
            MetaDataComponent? meta = null,
            bool reparent = true,
            bool force = false,
            EntityCoordinates? destination = null,
            Angle? localRotation = null)
        {
            foreach (var containers in Containers.Values)
            {
                if (containers.Contains(toremove))
                    return containers.Remove(toremove, _entMan, xform, meta, reparent, force, destination, localRotation);
            }

            return true; // If we don't contain the entity, it will always be removed
        }

        private IContainer MakeContainer(string id, Type type)
        {
            if (HasContainer(id)) throw new ArgumentException($"Container with specified ID already exists: '{id}'");

            var container = _dynFactory.CreateInstanceUnchecked<BaseContainer>(type);
            container.ID = id;
            container.Manager = this;

            Containers[id] = container;
            Dirty();
            return container;
        }

        public AllContainersEnumerable GetAllContainers()
        {
            return new(this);
        }

        [Serializable, NetSerializable]
        internal sealed class ContainerManagerComponentState : ComponentState
        {
            public Dictionary<string, ContainerData> Containers;

            public ContainerManagerComponentState(Dictionary<string, ContainerData> containers)
            {
                Containers = containers;
            }

            [Serializable, NetSerializable]
            public readonly struct ContainerData
            {
                public readonly string ContainerType;
                public readonly string Id;
                public readonly bool ShowContents;
                public readonly bool OccludesLight;
                public readonly EntityUid[] ContainedEntities;

                public ContainerData(string containerType, string id, bool showContents, bool occludesLight, EntityUid[] containedEntities)
                {
                    ContainerType = containerType;
                    Id = id;
                    ShowContents = showContents;
                    OccludesLight = occludesLight;
                    ContainedEntities = containedEntities;
                }

                public void Deconstruct(out string type, out string id, out bool showEnts, out bool occludesLight, out EntityUid[] ents)
                {
                    type = ContainerType;
                    id = Id;
                    showEnts = ShowContents;
                    occludesLight = OccludesLight;
                    ents = ContainedEntities;
                }
            }
        }

        [DataDefinition]
        private struct ContainerPrototypeData
        {
            [DataField("entities")] public List<EntityUid> Entities = new ();

            [DataField("type")] public string? Type = null;

            // explicit parameterless constructor is required.
            public ContainerPrototypeData() { }

            public ContainerPrototypeData(List<EntityUid> entities, string type)
            {
                Entities = entities;
                Type = type;
            }
        }

        public readonly struct AllContainersEnumerable : IEnumerable<IContainer>
        {
            private readonly ContainerManagerComponent? _manager;

            public AllContainersEnumerable(ContainerManagerComponent? manager)
            {
                _manager = manager;
            }

            public AllContainersEnumerator GetEnumerator()
            {
                return new(_manager);
            }

            IEnumerator<IContainer> IEnumerable<IContainer>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct AllContainersEnumerator : IEnumerator<IContainer>
        {
            private Dictionary<string, IContainer>.ValueCollection.Enumerator _enumerator;

            public AllContainersEnumerator(ContainerManagerComponent? manager)
            {
                _enumerator = manager?.Containers.Values.GetEnumerator() ?? new();
                Current = default;
            }

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    if (!_enumerator.Current.Deleted)
                    {
                        Current = _enumerator.Current;
                        return true;
                    }
                }

                return false;
            }

            void IEnumerator.Reset()
            {
                ((IEnumerator<IContainer>) _enumerator).Reset();
            }

            [AllowNull]
            public IContainer Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }
}
