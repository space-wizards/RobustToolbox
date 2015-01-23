using SS14.Shared;
using SS14.Shared.GameObjects;
using System.Collections.Generic;

namespace SS14.Server.GameObjects
{
    interface IInventoryContainer
    {
        bool RemoveEntity(Entity actor, Entity toRemove, InventoryLocation location = InventoryLocation.Any);
        bool AddEntity(Entity actor, Entity toAdd, InventoryLocation location = InventoryLocation.Any);
        IEnumerable<Entity> GetEntitiesInInventory();
    }
}
