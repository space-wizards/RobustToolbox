using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Holds data about a set of entity containers on this entity.
    /// </summary>
    [NetworkedComponent]
    [RegisterComponent, ComponentProtoName("ContainerContainer")]
    public sealed partial class ContainerManagerComponent : Component, ISerializationHooks
    {
        [Dependency] private readonly IDynamicTypeFactoryInternal _dynFactory = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("containers")]
        public Dictionary<string, BaseContainer> Containers = new();

        void ISerializationHooks.AfterDeserialization()
        {
            // TODO custom type serializer
            foreach (var (id, container) in Containers)
            {
                container.Manager = this;
                container.Owner = Owner;
                container.ID = id;
            }
        }

        public T MakeContainer<T>(EntityUid uid, string id)
            where T : BaseContainer
        {
            if (HasContainer(id))
                throw new ArgumentException($"Container with specified ID already exists: '{id}'");

            var container = _dynFactory.CreateInstanceUnchecked<T>(typeof(T), inject: false);
            container.Init(id, uid, this);
            Containers[id] = container;
            _entMan.Dirty(uid, this);
            return container;
        }

        public BaseContainer GetContainer(string id)
        {
            return Containers[id];
        }

        public bool HasContainer(string id)
        {
            return Containers.ContainsKey(id);
        }

        public bool TryGetContainer(string id, [NotNullWhen(true)] out BaseContainer? container)
        {
            var ret = Containers.TryGetValue(id, out var cont);
            container = cont!;
            return ret;
        }

        public bool TryGetContainer(EntityUid entity, [NotNullWhen(true)] out BaseContainer? container)
        {
            foreach (var contain in Containers.Values)
            {
                if (contain.Contains(entity))
                {
                    container = contain;
                    return true;
                }
            }

            container = default;
            return false;
        }

        public bool ContainsEntity(EntityUid entity)
        {
            foreach (var container in Containers.Values)
            {
                if (container.Contains(entity)) return true;
            }

            return false;
        }

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
                public readonly string ContainerType; // TODO remove this. We dont have to send a whole string.
                public readonly bool ShowContents;
                public readonly bool OccludesLight;
                public readonly NetEntity[] ContainedEntities;

                public ContainerData(string containerType, bool showContents, bool occludesLight, NetEntity[] containedEntities)
                {
                    ContainerType = containerType;
                    ShowContents = showContents;
                    OccludesLight = occludesLight;
                    ContainedEntities = containedEntities;
                }

                public void Deconstruct(out string type, out bool showEnts, out bool occludesLight, out NetEntity[] ents)
                {
                    type = ContainerType;
                    showEnts = ShowContents;
                    occludesLight = OccludesLight;
                    ents = ContainedEntities;
                }
            }
        }

        public readonly struct AllContainersEnumerable : IEnumerable<BaseContainer>
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

            IEnumerator<BaseContainer> IEnumerable<BaseContainer>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct AllContainersEnumerator : IEnumerator<BaseContainer>
        {
            private Dictionary<string, BaseContainer>.ValueCollection.Enumerator _enumerator;

            public AllContainersEnumerator(ContainerManagerComponent? manager)
            {
                _enumerator = manager?.Containers.Values.GetEnumerator() ?? new();
                Current = default;
            }

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    Current = _enumerator.Current;
                    return true;
                }

                return false;
            }

            void IEnumerator.Reset()
            {
                ((IEnumerator<BaseContainer>) _enumerator).Reset();
            }

            [AllowNull]
            public BaseContainer Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}
