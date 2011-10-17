using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace CGO
{
    public class EquipmentComponent : GameObjectComponent
    {
        public Dictionary<GUIBodyPart, Entity> equippedEntities = new Dictionary<GUIBodyPart, Entity>();
        public List<GUIBodyPart> activeSlots = new List<GUIBodyPart>();

        public EquipmentComponent()
        {
            family = ComponentFamily.Equipment;
        }
    }
}
