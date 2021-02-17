using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Containers
{
    [DebuggerDisplay("ClientContainer {Owner.Uid}/{ID}")]
    public sealed class ClientContainer : IContainer
    {
        public List<IEntity> Entities { get; } = new();

        public ClientContainer(string id, IContainerManager manager)
        {
            ID = id;
            Manager = manager;
        }

        [ViewVariables] public IContainerManager Manager { get; }
        [ViewVariables] public string ID { get; }
        [ViewVariables] public IEntity Owner => Manager.Owner;
        [ViewVariables] public bool Deleted { get; private set; }
        [ViewVariables] public IReadOnlyList<IEntity> ContainedEntities => Entities;
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
            return Entities.Contains(contained);
        }

        public void Shutdown()
        {
            Deleted = true;
        }
    }
}
