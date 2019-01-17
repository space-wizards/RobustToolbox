using SS14.Client.Graphics;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.ItemList))]
    public class ItemList : Control
    {
        public int ItemCount => (int)SceneControl.Call("get_item_count");

        public ItemList() : base() { }
        public ItemList(string name) : base(name) { }
        internal ItemList(Godot.ItemList control) : base(control) { }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ItemList();
        }

        public void AddItem(string text, Texture icon = null, bool selectable = true)
        {
            SceneControl.Call("add_item", text, icon, selectable);
        }

        public void AddIconItem(Texture icon, bool selectable = true)
        {
            SceneControl.Call("add_icon_item", icon, selectable);
        }

        public void Clear()
        {
            SceneControl.Call("clear");
        }

        public void EnsureCurrentIsVisible()
        {
            SceneControl.Call("ensure_current_is_visible");
        }

        public int GetItemAtPosition(Vector2 position, bool exact = false)
        {
            return (int)SceneControl.Call("get_item_at_position", position.Convert(), exact);
        }

        public bool IsSelected(int idx)
        {
            return (bool)SceneControl.Call("is_selected", idx);
        }

        public void RemoveItem(int idx)
        {
            SceneControl.Call("remove_item", idx);
        }

        public void Select(int idx, bool single = true)
        {
            SceneControl.Call("select", idx, single);
        }

        public void SetItemCustomBgColor(int idx, Color color)
        {
            SceneControl.Call("set_icon_custom_bg_color", idx, color.Convert());
        }

        public void SetItemDisabled(int idx, bool disabled)
        {
            SceneControl.Call("set_item_disabled", idx, disabled);
        }

        public void SetItemIcon(int idx, Texture icon)
        {
            SceneControl.Call("set_item_icon", idx, icon);
        }

        public void SetItemIconRegion(int idx, UIBox2 region)
        {
            SceneControl.Call("set_item_icon_region", idx, region.Convert());
        }

        public void SetItemSelectable(int idx, bool selectable)
        {
            SceneControl.Call("set_item_selectable", idx, selectable);
        }

        public void SetItemText(int idx, string text)
        {
            SceneControl.Call("set_item_text", idx, text);
        }

        public void SetItemTooltip(int idx, string tooltip)
        {
            SceneControl.Call("set_item_tooltip", idx, tooltip);
        }

        public void SetItemTooltipEnabled(int idx, bool enabled)
        {
            SceneControl.Call("set_item_tooltip_enabled", idx, enabled);
        }

        public void SortItemsByText()
        {
            SceneControl.Call("sort_items_by_text");
        }

        public void Unselect(int idx)
        {
            SceneControl.Call("unselect", idx);
        }
    }
}
