using System;
using SS14.Client.Graphics;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap("ItemList")]
    public class ItemList : Control
    {
        #if GODOT
        new Godot.ItemList SceneControl;
        #endif

        #if GODOT
        public int ItemCount => SceneControl.GetItemCount();
        #else
        public int ItemCount => throw new NotImplementedException();
        #endif

        public ItemList() : base() { }
        public ItemList(string name) : base(name) { }
        #if GODOT
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
        #endif

        public void AddItem(string text, Texture icon = null, bool selectable = true)
        {
            #if GODOT
            SceneControl.AddItem(text, icon, selectable);
            #endif
        }

        public void AddIconItem(Texture icon, bool selectable = true)
        {
            #if GODOT
            SceneControl.AddIconItem(icon, selectable);
            #endif
        }

        public void Clear()
        {
            #if GODOT
            SceneControl.Clear();
            #endif
        }

        public void EnsureCurrentIsVisible()
        {
            #if GODOT
            SceneControl.EnsureCurrentIsVisible();
            #endif
        }

        public int GetItemAtPosition(Vector2 position, bool exact = false)
        {
            #if GODOT
            return SceneControl.GetItemAtPosition(position.Convert(), exact);
            #else
            throw new NotImplementedException();
            #endif
        }

        public bool IsSelected(int idx)
        {
            #if GODOT
            return SceneControl.IsSelected(idx);
            #else
            throw new NotImplementedException();
            #endif
        }

        public void RemoveItem(int idx)
        {
            #if GODOT
            SceneControl.RemoveItem(idx);
            #endif
        }

        public void Select(int idx, bool single = true)
        {
            #if GODOT
            SceneControl.Select(idx, single);
            #endif
        }

        public void SetItemCustomBgColor(int idx, Color color)
        {
            #if GODOT
            SceneControl.SetItemCustomBgColor(idx, color.Convert());
            #endif
        }

        public void SetItemDisabled(int idx, bool disabled)
        {
            #if GODOT
            SceneControl.SetItemDisabled(idx, disabled);
            #endif
        }

        public void SetItemIcon(int idx, Texture icon)
        {
            #if GODOT
            SceneControl.SetItemIcon(idx, icon);
            #endif
        }

        public void SetItemIconRegion(int idx, UIBox2 region)
        {
            #if GODOT
            SceneControl.SetItemIconRegion(idx, region.Convert());
            #endif
        }

        public void SetItemSelectable(int idx, bool selectable)
        {
            #if GODOT
            SceneControl.SetItemSelectable(idx, selectable);
            #endif
        }

        public void SetItemText(int idx, string text)
        {
            #if GODOT
            SceneControl.SetItemText(idx, text);
            #endif
        }

        public void SetItemTooltip(int idx, string tooltip)
        {
            #if GODOT
            SceneControl.SetItemTooltip(idx, tooltip);
            #endif
        }

        public void SetItemTooltipEnabled(int idx, bool enabled)
        {
            #if GODOT
            SceneControl.SetItemTooltipEnabled(idx, enabled);
            #endif
        }

        public void SortItemsByText()
        {
            #if GODOT
            SceneControl.SortItemsByText();
            #endif
        }

        public void Unselect(int idx)
        {
            #if GODOT
            SceneControl.Unselect(idx);
            #endif
        }
    }
}
