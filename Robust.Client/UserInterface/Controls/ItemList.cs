using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Input;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using Color = Robust.Shared.Maths.Color;
using Font = Robust.Client.Graphics.Font;

namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.ItemList))]
    public class ItemList : Control
    {
        private bool _isAtBottom = true;
        private int _totalContentHeight;
        private float _itemListHeight;

        private VScrollBar _scrollBar = new VScrollBar();
        private readonly List<Item> _itemList = new List<Item>();
        public event Action<ItemListSelectedEventArgs> OnItemSelected;
        public event Action<ItemListDeselectedEventArgs> OnItemDeselected;
        public event Action<ItemListHoverEventArgs> OnItemHover;

        public const string StylePropertyBackground = "itemlist-background";
        public const string StylePropertyItemBackground = "item-background";
        public const string StylePropertySelectedItemBackground = "selected-item-background";
        public const string StylePropertyDisabledItemBackground = "disabled-item-background";

        public int ItemCount => GameController.OnGodot ? (int)SceneControl.Call("get_item_count") : _itemList.Count;

        public bool ScrollFollowing { get; set; } = true;

        private int ScrollLimit => Math.Max(0, _totalContentHeight - (int) _getContentBox().Height + 1);

        public ItemListSelectMode SelectMode { get; set; } = ItemListSelectMode.Single;

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

        private void RecalculateContentHeight()
        {
            _totalContentHeight = 0;
            foreach (var item in _itemList)
            {
                var itemHeight = item.Icon != null
                    ? Math.Max(item.IconSize.Y, ActualFont.Height) + ActualItemBackground.MinimumSize.Y
                    : ActualFont.Height + ActualItemBackground.MinimumSize.Y;

                _totalContentHeight += (int)itemHeight;
            }
        }

        public void AddItem(string text, Texture icon = null, bool selectable = true)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("add_item", text, icon, selectable);
            }
            else
            {
                var item = new Item() {Text = text, Icon = icon, Selectable = selectable};
                _itemList.Add(item);
                RecalculateContentHeight();
                if (_isAtBottom && ScrollFollowing)
                {
                    _scrollBar.Value = ScrollLimit;

                }
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
                _totalContentHeight = 0;
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
                _itemList.RemoveAt(idx);
                RecalculateContentHeight();
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
                    for (var jdx = 0; jdx < _itemList.Count; jdx++)
                    {
                        Unselect(jdx);
                    }
                }

                var i = _itemList[idx];

                if (i.Selectable)
                {
                    i.Selected = true;
                    OnItemSelected?.Invoke(new ItemListSelectedEventArgs(idx, this));
                }
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
                var idx = _itemList.IndexOf(item);
                if (idx != -1)
                    Select(idx, single);
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
                Unselect(idx);
                var i = _itemList[idx];
                i.Disabled = disabled;
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
                var i = _itemList[idx];
                if (!i.Selected) return;
                i.Selected = false;
                OnItemDeselected?.Invoke(new ItemListDeselectedEventArgs(idx, this));
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
                var idx = _itemList.IndexOf(item);
                if (idx == -1) return;
                Unselect(idx);
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

        public void ScrollToBottom()
        {
            _scrollBar.Value = ScrollLimit;
            _isAtBottom = true;
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

            _itemListHeight = 0f;
            Vector2 separation = (0, -_scrollBar.Value);

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

                item.Region = UIBox2.FromDimensions(separation, (SizeBox.Width, itemHeight));

                bg.Draw(handle, item.Region.Value);

                if (item.Icon != null)
                {
                    if (item.IconRegion.Size == Vector2.Zero)
                    {
                        handle.DrawTextureRect(item.Icon, UIBox2.FromDimensions(separation, item.Icon.Size), false, item.IconModulate, item.IconTranspose);
                    }
                    else
                    {
                        handle.DrawTextureRectRegion(item.Icon, UIBox2.FromDimensions(separation, item.Icon.Size), item.IconRegion, item.IconModulate);
                    }
                }

                if (item.Text != null)
                {
                    DrawTextInternal(handle, item.Text,
                        UIBox2.FromDimensions((item.IconSize.X, separation.Y), (SizeBox.Width-item.IconSize.X,font.Height*2))
                        );
                }

                separation += (0, itemHeight);
                _itemListHeight += itemHeight;
            }

            if (_itemListHeight > Size.Y)
            {
                _scrollBar.MaxValue = _itemListHeight;
                _scrollBar.Page = Size.Y - ActualBackground.MinimumSize.Y;
            }
            else
            {
                _scrollBar.MaxValue = 0f;
                _scrollBar.Page = 0f;
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

        protected override Vector2 CalculateMinimumSize()
        {
            if (_itemListHeight > ActualBackground?.MinimumSize.Y)
                return (ActualBackground?.MinimumSize.X ?? 0, _itemListHeight);
            return ActualBackground?.MinimumSize ?? Vector2.Zero;
        }

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            if (GameController.OnGodot)
            {
                return;
            }

            for (var idx = 0; idx < _itemList.Count; idx++)
            {
                var item = _itemList[idx];
                if (item.Region == null) continue;
                if (!item.Region.Value.Contains(args.RelativePosition)) continue;
                OnItemHover?.Invoke(new ItemListHoverEventArgs(idx, this));
                break;
            }
        }

        protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
        {
            base.MouseWheel(args);

            if (GameController.OnGodot)
            {
                return;
            }

            if (args.WheelDirection == Mouse.Wheel.Up)
            {
                _scrollBar.Value = _scrollBar.Value - _getScrollSpeed();
                _isAtBottom = false;
            }
            else if (args.WheelDirection == Mouse.Wheel.Down)
            {
                var limit = ScrollLimit;
                if (_scrollBar.Value + _getScrollSpeed() < limit)
                    _scrollBar.Value = _scrollBar.Value + _getScrollSpeed();
                else
                    ScrollToBottom();
                if (_scrollBar.IsAtEnd)
                {
                    _isAtBottom = true;
                }
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

        protected override void Initialize()
        {
            base.Initialize();
            if (GameController.OnGodot)
                return;

            _scrollBar = new VScrollBar {Name = "_v_scroll"};
            AddChild(_scrollBar);
            _scrollBar.SetAnchorAndMarginPreset(LayoutPreset.RightWide);
            _scrollBar.OnValueChanged += _ => _isAtBottom = _scrollBar.IsAtEnd;
        }

        [Pure]
        private int _getScrollSpeed()
        {
            var font = ActualFont;
            return font.Height * 2;
        }

        [Pure]
        private UIBox2 _getContentBox()
        {
            var style = ActualBackground;
            return style?.GetContentBox(SizeBox) ?? SizeBox;
        }

        public sealed class Item
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
        }

        public class ItemListEventArgs : EventArgs
        {
            /// <summary>
            ///     The ItemList this event originated from.
            /// </summary>
            public ItemList ItemList { get; }

            public ItemListEventArgs(ItemList list)
            {
                ItemList = list;
            }
        }

        public class ItemListSelectedEventArgs : ItemListEventArgs
        {
            /// <summary>
            ///     The index of the item that was selected.
            /// </summary>
            public int ItemIndex;

            public ItemListSelectedEventArgs(int itemIndex, ItemList list) : base(list)
            {
                ItemIndex = itemIndex;
            }
        }

        public class ItemListDeselectedEventArgs : ItemListEventArgs
        {
            /// <summary>
            ///     The index of the item that was selected.
            /// </summary>
            public int ItemIndex;

            public ItemListDeselectedEventArgs(int itemIndex, ItemList list) : base(list)
            {
                ItemIndex = itemIndex;
            }
        }

        public class ItemListHoverEventArgs : ItemListEventArgs
        {
            /// <summary>
            ///     The index of the item that was selected.
            /// </summary>
            public int ItemIndex;

            public ItemListHoverEventArgs(int itemIndex, ItemList list) : base(list)
            {
                ItemIndex = itemIndex;
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
