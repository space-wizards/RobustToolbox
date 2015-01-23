using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SS14.Server.GameObjects
{
    public class NewEquipmentComponent : Component
    {
        protected List<EquipmentSlot> activeSlots = new List<EquipmentSlot>();
        public Dictionary<EquipmentSlot, Entity> equippedEntities = new Dictionary<EquipmentSlot, Entity>();

        public NewEquipmentComponent()
        {
            Family = ComponentFamily.Equipment;
        }

        public override void HandleExtendedParameters(XElement extendedParameters)
        {
            activeSlots.Clear();

            foreach (XElement param in extendedParameters.Descendants("equipmentSlot"))
            {
                if (param.Attribute("slot") != null)
                    activeSlots.Add((EquipmentSlot)Enum.Parse(typeof(EquipmentSlot), param.Attribute("slot").Value));
            }
        }

        public bool RemoveEntity(Entity user, Entity toRemove)
        {
            if (equippedEntities.Any(x => x.Value == toRemove))
            {
                EquippableComponent eqCompo = toRemove.GetComponent<EquippableComponent>(ComponentFamily.Equippable);

                if(eqCompo != null)
                    eqCompo.currentWearer = null;

                equippedEntities[equippedEntities.First(x => x.Value == toRemove).Key] = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AddEntity(Entity user, Entity toAdd)
        {
            if (equippedEntities.Any(x => x.Value == toAdd))
                return false;
            else
            {
                EquippableComponent eqCompo = toAdd.GetComponent<EquippableComponent>(ComponentFamily.Equippable);
                if (eqCompo != null)
                {
                    if (activeSlots.Contains(eqCompo.wearloc) && !equippedEntities.ContainsKey(eqCompo.wearloc))
                    {
                        equippedEntities.Add(eqCompo.wearloc, toAdd);
                        eqCompo.currentWearer = this.Owner;

                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
        }

        public override ComponentState GetComponentState()
        {
            Dictionary<EquipmentSlot, int> equipped = equippedEntities.Select(x => new KeyValuePair<EquipmentSlot, int>(x.Key, x.Value.Uid)).ToDictionary(key => key.Key, va => va.Value);
            return new EquipmentComponentState(equipped, activeSlots);
        }
    }
}