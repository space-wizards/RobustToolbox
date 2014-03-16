using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameObject;
using SS13_Shared;

namespace SGO
{
    interface IInventoryContainer
    {
        bool RemoveEntity(Entity actor, Entity toRemove, InventoryLocation location = InventoryLocation.Any);
        bool AddEntity(Entity actor, Entity toAdd, InventoryLocation location = InventoryLocation.Any);
        IEnumerable<Entity> GetEntitiesInInventory();
    }
}
