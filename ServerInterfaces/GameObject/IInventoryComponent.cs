using System.Collections.Generic;
namespace ServerInterfaces.GameObject
{
    public interface IInventoryComponent
    {
        List<IEntity> containedEntities { get; }
        int maxSlots { get; }
        bool containsEntity(IEntity toSearch);
    }
}