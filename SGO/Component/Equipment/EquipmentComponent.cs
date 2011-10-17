using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO
{
    public class EquipmentComponent : GameObjectComponent
    {
        protected Dictionary<GUIBodyPart, Entity> equippedEntities = new Dictionary<GUIBodyPart,Entity>();
        protected List<GUIBodyPart> activeSlots = new List<GUIBodyPart>();

        public EquipmentComponent()
        {
            family = ComponentFamily.Equipment;

        }

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.EquipItem: //Equip an entity straight up.
                    EquipEntity((GUIBodyPart)list[0], (Entity)list[1]);
                    break;
                case ComponentMessageType.EquipItemInHand: //Move an entity from a hand to an equipment slot
                    if (!Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Hands))
                        return; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT
                    List<ComponentReplyMessage> reps = new List<ComponentReplyMessage>();
                    //Get the item in the hand
                    Owner.SendMessage(this, ComponentMessageType.GetActiveHandItem, reps);
                    if (reps.Count > 0 && reps[0].messageType == ComponentMessageType.ReturnActiveHandItem)
                    {
                        //Remove from hand
                        Owner.SendMessage(this, ComponentMessageType.DropItemInCurrentHand, null);
                        //Equip
                        EquipEntity((GUIBodyPart)list[0], (Entity)reps[0].paramsList[0]);
                    }
                    break;
                case ComponentMessageType.UnEquipItemToFloor: //remove an entity from a slot and drop it on the floor
                    UnEquipEntity((Entity)list[0]);
                    break;
                case ComponentMessageType.UnEquipItemToHand: //remove an entity from a slot and put it in the current hand slot.
                    if (!Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Hands))
                        return; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT

                    break;
            }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {

        }

        private void EquipEntity(GUIBodyPart part, Entity e)
        {
            if (IsItem(e) && IsEmpty(part) && e != null && activeSlots.Contains(part)) //If the part is empty, the part exists on this mob, and the entity specified is not null
            {
                equippedEntities.Add(part, e);
                e.SendMessage(this, SS3D_shared.GO.ComponentMessageType.ItemEquipped, null);
            }
        }

        private bool IsItem(Entity e)
        {
            if (e.HasComponent(SS3D_shared.GO.ComponentFamily.Item)) //We can only equip items derp
                return true;
            return false;
        }

        private void UnEquipEntity(GUIBodyPart part)
        {
            if (!IsEmpty(part)) //If the part is not empty
            {
                equippedEntities[part].SendMessage(this, SS3D_shared.GO.ComponentMessageType.ItemUnEquipped, null);
                equippedEntities.Remove(part);
            }
        }

        private void UnEquipEntity(Entity e)
        {
            GUIBodyPart key;
            foreach (var kvp in equippedEntities)
            {
                if(kvp.Value == e)
                {
                    key = kvp.Key;
                    UnEquipEntity(key);
                    break;
                }
            }
        }
        
        private void UnEquipAllEntities()
        {
            foreach (Entity e in equippedEntities.Values)
            {
                e.SendMessage(this, SS3D_shared.GO.ComponentMessageType.ItemUnEquipped, null);
            }
            equippedEntities.Clear();
        }

        private Entity GetEntity(GUIBodyPart part)
        {
            if (!IsEmpty(part))
                return equippedEntities[part];
            else
                return null;
        }

        private bool IsEmpty(GUIBodyPart part)
        {
            if (equippedEntities.ContainsKey(part))
                return false;
            return true;
        }
    }
}
