using SS14.Client.Graphics;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.ItemList))]
    public class ItemList : Control
    {
        new Godot.ItemList SceneControl;

        public int ItemCount => SceneControl.GetItemCount();

        public ItemList() : base() { }
        public ItemList(string name) : base(name) { }
        internal ItemList(Godot.ItemList control) : base(control) { }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ItemList();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.ItemList)control;
        }

        public void AddItem(string text, Texture icon = null, bool selectable = true)
        {
            SceneControl.AddItem(text, icon, selectable);
        }

        public void AddIconItem(Texture icon, bool selectable = true)
        {
            SceneControl.AddIconItem(icon, selectable);
        }

        public void Clear()
        {
            SceneControl.Clear();
        }

        public void EnsureCurrentIsVisible()
        {
            SceneControl.EnsureCurrentIsVisible();
        }

        public int GetItemAtPosition(Vector2 position, bool exact = false)
        {
            return SceneControl.GetItemAtPosition(position.Convert(), exact);
        }

        public bool IsSelected(int idx)
        {
            return SceneControl.IsSelected(idx);
        }

        public void RemoveItem(int idx)
        {
            SceneControl.RemoveItem(idx);
        }

        public void Select(int idx, bool single = true)
        {
            SceneControl.Select(idx, single);
        }

        public void SetItemCustomBgColor(int idx, Color color)
        {
            SceneControl.SetItemCustomBgColor(idx, color.Convert());
        }

        public void SetItemDisabled(int idx, bool disabled)
        {
            SceneControl.SetItemDisabled(idx, disabled);
        }

        public void SetItemIcon(int idx, Texture icon)
        {
            SceneControl.SetItemIcon(idx, icon);
        }

        public void SetItemIconRegion(int idx, UIBox2 region)
        {
            SceneControl.SetItemIconRegion(idx, region.Convert());
        }

        public void SetItemSelectable(int idx, bool selectable)
        {
            SceneControl.SetItemSelectable(idx, selectable);
        }

        public void SetItemText(int idx, string text)
        {
            SceneControl.SetItemText(idx, text);
        }

        public void SetItemTooltip(int idx, string tooltip)
        {
            SceneControl.SetItemTooltip(idx, tooltip);
        }

        public void SetItemTooltipEnabled(int idx, bool enabled)
        {
            SceneControl.SetItemTooltipEnabled(idx, enabled);
        }

        public void SortItemsByText()
        {
            SceneControl.SortItemsByText();
        }

        public void Unselect(int idx)
        {
            SceneControl.Unselect(idx);
        }
    }
}
