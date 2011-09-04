using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_shared
{
    public enum GuiComponentType
    {
        HealthComponent,
        AppendagesComponent,
        StatPanelComponent,
        WindowComponent,
        HumanInventory
    }

    public enum HealthComponentMessage
    {
        CurrentHealth
    }

    public enum InventoryComponentMessage
    {
        Equip,
        Unequip
    }

    public enum HandsComponentMessage
    {
        SelectHand,
        UpdateHandObjects
    }
}
