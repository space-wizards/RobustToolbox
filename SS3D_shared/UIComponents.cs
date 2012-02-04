using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared
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
        CraftAlreadyCrafting,
        CraftNoRecipe
    }
}
