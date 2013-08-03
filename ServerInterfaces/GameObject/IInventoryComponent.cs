using System.Collections.Generic;
using GameObject;

namespace ServerInterfaces.GOC
{
    public interface IInventoryComponent
    {
        List<Entity> containedEntities { get; }
        int maxSlots { get; }
        bool containsEntity(Entity toSearch);
    }
}