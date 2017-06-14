using SS14.Shared.Interfaces.GameObjects;
using System.Collections.Generic;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IInventoryComponent
    {
        List<IEntity> containedEntities { get; }
        int maxSlots { get; }
        bool containsEntity(IEntity toSearch);
    }
}
