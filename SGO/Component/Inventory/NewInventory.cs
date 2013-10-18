using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Linq.Expressions;
using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.GOC;
using SS13_Shared.GO.Inventory;

namespace SGO
{
    public class NewInventoryComponent : Component
    {
        public NewInventoryComponent()
        {
            Family = ComponentFamily.Inventory;
            containedEntities = new List<Entity>();
        }

        public List<Entity> containedEntities { get; private set; }

        public int maxSlots { get; private set; }

        public override void HandleExtendedParameters(XElement extendedParameters)
        {
            foreach (XElement param in extendedParameters.Descendants("InventoryProperties"))
            {
                if (param.Attribute("inventorySize") != null)
                    maxSlots = int.Parse(param.Attribute("inventorySize").Value);

                //TODO: Add support for objects that are created inside inventories (Lockers, crates etc)
            }
        }

        public bool RemoveEntity(Entity user, Entity toRemove)
        {
            if (containedEntities.Contains(toRemove))
            {
                containedEntities.Remove(toRemove);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AddEntity(Entity user, Entity toAdd)
        {
            if (containedEntities.Contains(toAdd))
            {
                return false;
            }
            else
            {
                containedEntities.Add(toAdd);
                return true;
            }
        }

        public override ComponentState GetComponentState()
        {
            List<int> entities = containedEntities.Select(x => x.Uid).ToList();
            return new InventoryState(maxSlots, entities);
        }
    }
}