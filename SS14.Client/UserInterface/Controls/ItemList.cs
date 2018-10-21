using System;
using SS14.Client.Graphics;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.ItemList))]
    public class ItemList : Control
    {
        new Godot.ItemList SceneControl;
        public int ItemCount => GameController.OnGodot ? SceneControl.GetItemCount() : default;

        public ItemList() : base()
        {
        }

        public ItemList(string name) : base(name)
        {
        }

        internal ItemList(Godot.ItemList control) : base(control)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ItemList();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.ItemList) control;
        }

        public void AddItem(string text, Texture icon = null, bool selectable = true)
        {
            if (GameController.OnGodot)
            {
                SceneControl.AddItem(text, icon, selectable);
            }
        }

        public void AddIconItem(Texture icon, bool selectable = true)
        {
            if (GameController.OnGodot)
            {
                SceneControl.AddIconItem(icon, selectable);
            }
        }

        public void Clear()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Clear();
            }
        }

        public void EnsureCurrentIsVisible()
        {
            if (GameController.OnGodot)
            {
                SceneControl.EnsureCurrentIsVisible();
            }
        }

        public int GetItemAtPosition(Vector2 position, bool exact = false)
        {
            return GameController.OnGodot ? SceneControl.GetItemAtPosition(position.Convert(), exact) : default;

        }

        public bool IsSelected(int idx)
        {
            return GameController.OnGodot ? SceneControl.IsSelected(idx): default;
        }

        public void RemoveItem(int idx)
        {
            if (GameController.OnGodot)
            {
                SceneControl.RemoveItem(idx);
            }
        }

        public void Select(int idx, bool single = true)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Select(idx, single);
            }
        }

        public void SetItemCustomBgColor(int idx, Color color)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemCustomBgColor(idx, color.Convert());
            }
        }

        public void SetItemDisabled(int idx, bool disabled)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemDisabled(idx, disabled);
            }
        }

        public void SetItemIcon(int idx, Texture icon)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemIcon(idx, icon);
            }
        }

        public void SetItemIconRegion(int idx, UIBox2 region)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemIconRegion(idx, region.Convert());
            }
        }

        public void SetItemSelectable(int idx, bool selectable)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemSelectable(idx, selectable);
            }
        }

        public void SetItemText(int idx, string text)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemText(idx, text);
            }
        }

        public void SetItemTooltip(int idx, string tooltip)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemTooltip(idx, tooltip);
            }
        }

        public void SetItemTooltipEnabled(int idx, bool enabled)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemTooltipEnabled(idx, enabled);
            }
        }

        public void SortItemsByText()
        {
            if (GameController.OnGodot)
            {
                SceneControl.SortItemsByText();
            }
        }

        public void Unselect(int idx)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Unselect(idx);
            }
        }
    }
}
