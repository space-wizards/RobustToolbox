using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Containers;

/// <summary>
/// Holds data about a set of entity containers on this entity.
/// </summary>
[NetworkedComponent]
[RegisterComponent, ComponentProtoName("ContainerContainer")]
public sealed partial class ContainerManagerComponent : Component, ISerializationHooks
{
    /// <summary>
    /// Dictionary containing the containers on this entity.
    /// The key is used as an identifier and can be freely chosen when a new container is added with <see cref="SharedContainerSystem.MakeContainer"/>.
    /// </summary>
    [DataField]
    public Dictionary<string, BaseContainer> Containers = new();

        // Waiting on everything to use EntityBuilder, then it's gone.
        void ISerializationHooks.AfterDeserialization()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (Owner == EntityUid.Invalid)
                return;
#pragma warning restore CS0618 // Type or member is obsolete

            foreach (var (id, container) in Containers)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                container.Init(null!, id, (Owner, this));
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
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
