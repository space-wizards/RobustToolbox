using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.Component.Equipment
{
    public class EquipmentComponent : GameObjectComponent
    {
        private Dictionary<GUIBodyPart, Entity> equippedEntities;

        public EquipmentComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Equipment;

            equippedEntities = new Dictionary<GUIBodyPart,Entity>();
        }

        private void EquipEntity(GUIBodyPart part, Entity e)
        {

        }

        private void UnEquipEntity(GUIBodyPart part)
        {

        }

        private void UnEquipEntity(Entity e)
        {

        }

        private void UnEquipEntity(GUIBodyPart part, Entity e)
        {

        }

        private void UnEquipAllEntities()
        {

        }

        private void GetEntity(GUIBodyPart part)
        {

        }


    }
}
