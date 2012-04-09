namespace SS13_Shared
{
    public enum GuiComponentType
    {
        Undefined, //Make sure this is always the first. (As it is the default for gui components without assigned type)
        HandsUi,
        HealthComponent,
        AppendagesComponent,
        StatPanelComponent,
        WindowComponent,
        ComboGui,
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
