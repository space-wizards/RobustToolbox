using SS14.Shared;
using SS14.Shared.Interfaces.GameObjects;
using System.Collections.Generic;

namespace SS14.Server.GameObjects
{
    interface IInventoryContainer
    {
        bool RemoveEntity(IEntity actor, IEntity toRemove, InventoryLocation location = InventoryLocation.Any);
        bool AddEntity(IEntity actor, IEntity toAdd, InventoryLocation location = InventoryLocation.Any);
        IEnumerable<IEntity> GetEntitiesInInventory();
    }
}
