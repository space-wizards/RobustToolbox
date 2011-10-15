using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO
{
    public class EquipmentComponent : GameObjectComponent
    {
        protected Dictionary<GUIBodyPart, Entity> equippedEntities = new Dictionary<GUIBodyPart,Entity>();
        protected List<GUIBodyPart> activeSlots = new List<GUIBodyPart>();

        public EquipmentComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Equipment;

        }

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
            }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {

        }

        private void EquipEntity(GUIBodyPart part, Entity e)
        {
            if (IsEmpty(part) && e != null && activeSlots.Contains(part))
            {
                equippedEntities.Add(part, e);
                e.SendMessage(this, SS3D_shared.GO.ComponentMessageType.ItemEquipped, null);
            }
        }

        private void UnEquipEntity(GUIBodyPart part)
        {
            if (!IsEmpty(part))
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
