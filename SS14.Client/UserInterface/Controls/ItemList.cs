using System;
using System.Collections.Generic;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Input;
using SS14.Client.Utility;
using SS14.Shared.Log;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.ItemList))]
    public class ItemList : Control
    {
        public const string StylePropertyBackground = "itemlist-background";
        public const string StylePropertyItemBackground = "item-background";
        public const string StylePropertySelectedItemBackground = "selected-item-background";
        public const string StylePropertyDisabledItemBackground = "disabled-item-background";

        public int ItemCount => GameController.OnGodot ? (int)SceneControl.Call("get_item_count") : _itemList.Count;
        public ItemListSelectMode SelectMode = ItemListSelectMode.Single;

        private readonly List<Item> _itemList = new List<Item>();

        public ItemList()
        {
        }

        public ItemList(string name) : base(name)
        {
        }

        internal ItemList(Godot.ItemList control) : base(control)
        {
        }

        protected override void SetDefaults()
        {
            RectClipContent = true;
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ItemList();
        }

        public void AddItem(string text, Texture icon = null, bool selectable = true)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("add_item", text, icon, selectable);
            }
            else
            {
                var i = new Item() {Text = text, Icon = icon, Selectable = selectable};
                _itemList.Add(i);
            }
        }

        public void AddIconItem(Texture icon, bool selectable = true)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("add_icon_item", icon, selectable);
            }
            else
            {
                AddItem(null, icon, selectable);
            }
        }

        public void Clear()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("clear");
            }
            else
            {
                _itemList.Clear();
            }
        }

        public void EnsureCurrentIsVisible()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("ensure_current_is_visible");
            }
        }

        public int GetItemAtPosition(Vector2 position, bool exact = false)
        {
            return GameController.OnGodot ? (int)SceneControl.Call("get_item_at_position", position.Convert(), exact) : default;
        }

        public bool IsSelected(int idx)
        {
            return GameController.OnGodot ? (bool)SceneControl.Call("is_selected", idx) : _itemList[idx].Selected;
        }

        public void RemoveItem(int idx)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("remove_item", idx);
            }
            else
            {
                var i = _itemList[idx];
                _itemList.Remove(i);
                i.Dispose();
            }
        }

        public void Select(int idx, bool single = true)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("select", idx, single);
            }
            else
            {
                if (single)
                {
                    foreach (var item in _itemList)
                    {
                        item.Selected = false;
                    }
                }

                var i = _itemList[idx];

                if (i.Selectable)
                    i.Selected = true;
            }
        }

        public void Select(Item item, bool single = true)
        {
            if (GameController.OnGodot)
            {
                return;
            }
            else
            {
                if (single)
                {
                    foreach (var i in _itemList)
                    {
                        i.Selected = false;
                    }
                }

                if (item.Selectable)
                    item.Selected = true;
            }
        }

        public void SetItemCustomBgColor(int idx, Color color)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_icon_custom_bg_color", idx, color.Convert());
            }
            else
            {
                //_itemList[idx].CustomBg = color;
            }
        }

        public void SetItemDisabled(int idx, bool disabled)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_disabled", idx, disabled);
            }
            else
            {
                _itemList[idx].Disabled = disabled;
            }
        }

        public void SetItemIcon(int idx, Texture icon)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_icon", idx, icon);
            }
            else
            {
                _itemList[idx].Icon = icon;
            }
        }

        public void SetItemIconRegion(int idx, UIBox2 region)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_icon_region", idx, region.Convert());
            }
            else
            {
                _itemList[idx].IconRegion = region;
            }
        }

        public void SetItemSelectable(int idx, bool selectable)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_selectable", idx, selectable);
            }
            else
            {
                _itemList[idx].Selectable = selectable;
            }
        }

        public void SetItemText(int idx, string text)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_text", idx, text);
            }
            else
            {
                _itemList[idx].Text = text;
            }
        }

        public void SetItemTooltip(int idx, string tooltip)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_tooltip", idx, tooltip);
            }
            else
            {
                _itemList[idx].TooltipText = tooltip;
            }
        }

        public void SetItemTooltipEnabled(int idx, bool enabled)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_tooltip_enabled", idx, enabled);
            }
            else
            {
                _itemList[idx].TooltipEnabled = true;
            }
        }

        public void SortItemsByText()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("sort_items_by_text");
            }
            else
            {
                _itemList.Sort((p, q) => string.Compare(p.Text, q.Text, StringComparison.Ordinal));
            }
        }

        public void Unselect(int idx)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("unselect", idx);
            }
            else
            {
                _itemList[idx].Selected = false;
            }
        }

        public void Unselect(Item item)
        {
            if (GameController.OnGodot)
            {
                return;
            }
            else
            {
                if (_itemList.Contains(item))
                    item.Selected = false;
            }
        }

        public Font ActualFont
        {
            get
            {
                if (TryGetStyleProperty("font", out Font font))
                {
                    return font;
                }

                return UserInterfaceManager.ThemeDefaults.DefaultFont;
            }
        }

        public Color ActualFontColor
        {
            get
            {
                if (TryGetStyleProperty("font-color", out Color fontColor))
                {
                    return fontColor;
                }

                return Color.White;
            }
        }

        public StyleBox ActualBackground
        {
            get
            {
                if (TryGetStyleProperty(StylePropertyBackground, out StyleBox bg))
                {
                    return bg;
                }

                return new StyleBoxFlat();
            }
        }
        public StyleBox ActualItemBackground
        {
            get
            {
                if (TryGetStyleProperty(StylePropertyItemBackground, out StyleBox bg))
                {
                    return bg;
                }

                return new StyleBoxFlat();
            }
        }

        public StyleBox ActualSelectedItemBackground
        {
            get
            {
                if (TryGetStyleProperty(StylePropertySelectedItemBackground, out StyleBox bg))
                {
                    return bg;
                }

                return new StyleBoxFlat();
            }
        }

        public StyleBox ActualDisabledItemBackground
        {
            get
            {
                if (TryGetStyleProperty(StylePropertyDisabledItemBackground, out StyleBox bg))
                {
                    return bg;
                }

                return new StyleBoxFlat();
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (GameController.OnGodot)
            {
                return;
            }

            var font = ActualFont;
            var listBg = ActualBackground;
            var iconBg = ActualItemBackground;
            var iconSelectedBg = ActualSelectedItemBackground;
            var iconDisabledBg = ActualDisabledItemBackground;

            Vector2 separation = (0, 0);

            listBg.Draw(handle, SizeBox);

            foreach (var item in _itemList)
            {
                var bg = iconBg;

                if (item.Disabled)
                    bg = iconDisabledBg;

                if (item.Selected)
                    bg = iconSelectedBg;

                var itemHeight = item.Icon != null
                    ? Math.Max(item.IconSize.Y, font.Height) + bg.MinimumSize.Y
                    : font.Height + bg.MinimumSize.Y;

                item.Region = UIBox2.FromDimensions(Position + separation, (SizeBox.Width, itemHeight));

                bg.Draw(handle, item.Region.Value);

                if (item.Icon != null)
                {
                    if (item.IconRegion.Size == Vector2.Zero)
                    {
                        handle.DrawTextureRect(item.Icon, UIBox2.FromDimensions(Position + separation, item.Icon.Size), false, item.IconModulate, item.IconTranspose);
                    }
                    else
                    {
                        handle.DrawTextureRectRegion(item.Icon, UIBox2.FromDimensions(Position + separation, item.Icon.Size), item.IconRegion, item.IconModulate);
                    }
                }

                if (item.Text != null)
                {
                    DrawTextInternal(handle, item.Text,
                        UIBox2.FromDimensions(Position + (item.IconSize.X, separation.Y), (SizeBox.Width-item.IconSize.X,font.Height*2))
                        );
                }

                separation += (0, itemHeight);
            }
        }

        protected void DrawTextInternal(DrawingHandleScreen handle, string text, UIBox2 box)
        {
            var font = ActualFont;

            var color = ActualFontColor;
            var offsetY = (int) (box.Height - font.Height) / 2;
            var baseLine = new Vector2i(0, offsetY + font.Ascent) + box.TopLeft;

            foreach (var chr in text)
            {
                if (!font.TryGetCharMetrics(chr, out var metrics))
                {
                    continue;
                }

                if (!(baseLine.X < box.Left || baseLine.X + metrics.Advance > box.Right))
                {
                    font.DrawChar(handle, chr, baseLine, color);
                }

                baseLine += (metrics.Advance, 0);
            }
        }

        protected internal override void MouseDown(GUIMouseButtonEventArgs args)
        {
            base.MouseDown(args);

            if (GameController.OnGodot || SelectMode == ItemListSelectMode.None || args.Button != Mouse.Button.Left)
            {
                return;
            }

            foreach (var item in _itemList)
            {
                if (item.Region == null) continue;
                if (!item.Region.Value.Contains(args.RelativePosition)) continue;
                if (item.Selectable && !item.Disabled)
                    if (item.Selected)
                        Unselect(item);
                    else
                        Select(item, SelectMode == ItemListSelectMode.Single);
                break;
            }
        }

        public sealed class Item : IDisposable
        {
            public string Text = null;
            public string TooltipText = null;
            public Texture Icon = null;
            public UIBox2 IconRegion = new UIBox2();
            public Color IconModulate = Color.White;
            public bool IconTranspose = false;
            public bool Selected = false;
            public bool Selectable = true;
            public bool TooltipEnabled = true;
            public bool Disabled = false;

            public UIBox2? Region = null;

            public Vector2 IconSize
            {
                get
                {
                    if (Icon == null)
                        return Vector2.Zero;
                    return IconRegion.Size != Vector2.Zero ? IconRegion.Size : Icon.Size;
                }
            }

            public void Dispose()
            {
                Icon = null;
            }
        }

        public enum ItemListSelectMode
        {
            None,
            Single,
            Multiple,
        }
    }
}
