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
        ComboGUI,
        HumanInventory
    }

    public enum ComboGuiMessage
    {
        UpdateHands,
        SelectHand,
        CancelCraftBar,
        ShowCraftBar,
        CraftNeedInventorySpace,
        CraftSuccess,
        CraftItemsMissing,
        CraftNoRecipe
    }
}
