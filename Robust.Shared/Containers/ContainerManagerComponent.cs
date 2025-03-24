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
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("containers")]
        public Dictionary<string, BaseContainer> Containers = new();

        // Requires a custom serializer + copier to get rid of. Good luck
        void ISerializationHooks.AfterDeserialization()
        {
            foreach (var (id, container) in Containers)
            {
                container.Init(default!, id, (Owner, this));
            }
        }

        [Obsolete]
        public bool TryGetContainer(string id, [NotNullWhen(true)] out BaseContainer? container)
            => _entMan.System<SharedContainerSystem>().TryGetContainer(Owner, id, out container, this);

        [Obsolete]
        public bool TryGetContainer(EntityUid entity, [NotNullWhen(true)] out BaseContainer? container)
            => _entMan.System<SharedContainerSystem>().TryGetContainingContainer(Owner, entity, out container, this);

        [Obsolete]
        public AllContainersEnumerable GetAllContainers()
            => _entMan.System<SharedContainerSystem>().GetAllContainers(Owner, this);

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
