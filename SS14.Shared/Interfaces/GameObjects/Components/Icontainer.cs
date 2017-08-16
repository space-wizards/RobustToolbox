using SS14.Server.GameObjects.Components.Container;
using SS14.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IContainer
    {
        bool CanInsert(IEntity toinsert);
        bool Insert(IEntity toinsert);
        bool CanRemove(IEntity toremove);
        bool Remove(IEntity toremove);
        bool Contains(IEntity contained);
        void Shutdown();
    }
}
