using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameObjects;

namespace SS14.Server.GameObjects.Components.Container
{
    public class Container : List<Entity>, IContainer
    {
        public bool CanInsert(Entity toinsert)
        {
            return true;
        }
        public bool Insert(Entity toinsert)
        {
            if (CanInsert(toinsert))
            {
                this.Add(toinsert);
                return true;
            }
            return false;
        }
        public bool CanRemove(Entity toremove)
        {
            return true;
        }

        public new bool Remove(Entity toremove)
        {
            if (CanRemove(toremove))
            {
                base.Remove(toremove);
                return true;
            }
            return false;
        }
    }
}
