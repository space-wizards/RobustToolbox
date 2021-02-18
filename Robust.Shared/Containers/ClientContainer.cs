using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Containers
{
    [DebuggerDisplay("ClientContainer {Owner.Uid}/{ID}")]
    [SerializedType("ClientContainer")]
    public sealed class ClientContainer : IContainer, IExposeData
    {
        private List<IEntity> _entities = new();

        public ClientContainer(string id, IContainerManager manager)
        {
            ID = id;
            Manager = manager;
        }

        [ViewVariables] public IContainerManager Manager { get; }
        [ViewVariables] public string ID { get; }
        [ViewVariables] public IEntity Owner => Manager.Owner;
        [ViewVariables] public bool Deleted { get; private set; }
        [ViewVariables] public IReadOnlyList<IEntity> ContainedEntities => _entities;
        [ViewVariables]
        public bool ShowContents { get; set; }
        [ViewVariables]
        public bool OccludesLight { get; set; }

        public bool CanInsert(IEntity toinsert)
        {
            return false;
        }

        public bool Insert(IEntity toinsert)
        {
            return false;
        }

        public bool CanRemove(IEntity toremove)
        {
            return false;
        }

        public bool Remove(IEntity toremove)
        {
            return false;
        }

        public void ForceRemove(IEntity toRemove)
        {
            throw new NotSupportedException("Cannot directly modify containers on the client");
        }

        public bool Contains(IEntity contained)
        {
            return _entities.Contains(contained);
        }

        public void Shutdown()
        {
            Deleted = true;
        }

        /// <inheritdoc />
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataReadWriteFunction("showEnts", false, value => ShowContents = value, () => ShowContents);
            serializer.DataReadWriteFunction("occludes", false, value => OccludesLight = value, () => OccludesLight);
            serializer.DataField(ref _entities, "ents", new List<IEntity>());
        }
    }
}
