using SS14.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IContainer
    {
        bool CanInsert(Entity toinsert);
        bool Insert(Entity toinsert);
        bool CanRemove(Entity toremove);
        bool Remove(Entity toremove);
    }
}
