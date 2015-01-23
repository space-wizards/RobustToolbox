using SS14.Shared.GameObjects;
using System.Collections.Generic;

namespace SS14.Server.Interfaces.GOC
{
    public interface IInventoryComponent
    {
        List<Entity> containedEntities { get; }
        int maxSlots { get; }
        bool containsEntity(Entity toSearch);
    }
}