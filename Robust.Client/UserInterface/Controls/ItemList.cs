using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class ItemList : Control
    {
        private bool _isAtBottom = true;
        private int _totalContentHeight;

        private VScrollBar _scrollBar = new VScrollBar();
        private readonly List<Item> _itemList = new List<Item>();
        public event Action<ItemListSelectedEventArgs> OnItemSelected;
        public event Action<ItemListDeselectedEventArgs> OnItemDeselected;
        public event Action<ItemListHoverEventArgs> OnItemHover;

        public const string StylePropertyBackground = "itemlist-background";
        public const string StylePropertyItemBackground = "item-background";
        public const string StylePropertySelectedItemBackground = "selected-item-background";
        public const string StylePropertyDisabledItemBackground = "disabled-item-background";

        public int ItemCount => _itemList.Count;

        public bool ScrollFollowing { get; set; } = true;

        public ItemListSelectMode SelectMode { get; set; } = ItemListSelectMode.Single;

        public ItemList()
        {
            RectClipContent = true;

            _scrollBar = new VScrollBar {Name = "_v_scroll"};
            AddChild(_scrollBar);
            _scrollBar.SetAnchorAndMarginPreset(LayoutPreset.RightWide);
            _scrollBar.OnValueChanged += _ => _isAtBottom = _scrollBar.IsAtEnd;
        }

        private void RecalculateContentHeight()
        {
            _totalContentHeight = 0;
            foreach (var item in _itemList)
            {
                var itemHeight = 0f;
                if (item.Icon != null)
                {
                    itemHeight = item.IconSize.Y;
                }

                itemHeight = Math.Max(itemHeight, ActualFont.GetHeight(UIScale));
                itemHeight += ActualItemBackground.MinimumSize.Y;

                _totalContentHeight += (int)Math.Ceiling(itemHeight);
            }

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            _updateScrollbarVisibility();
        }

        public void AddItem(string text, Texture icon = null, bool selectable = true)
        {
            var item = new Item {Text = text, Icon = icon, Selectable = selectable};
            _itemList.Add(item);
            RecalculateContentHeight();
            if (_isAtBottom && ScrollFollowing)
            {
                _scrollBar.MoveToEnd();
            }
        }

        public void AddIconItem(Texture icon, bool selectable = true)
        {
            AddItem(null, icon, selectable);
        }

        public void Clear()
        {
            _itemList.Clear();
            _totalContentHeight = 0;
        }

        public void EnsureCurrentIsVisible()
        {
            // TODO: Implement this.
        }

        public int GetItemAtPosition(Vector2 position, bool exact = false)
        {
            throw new NotImplementedException();
        }

        public bool IsSelected(int idx)
        {
            return _itemList[idx].Selected;
        }

        public void RemoveItem(int idx)
        {
            _itemList.RemoveAt(idx);
            RecalculateContentHeight();
        }

        public void Select(int idx, bool single = true)
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

        public void Select(Item item, bool single = true)
        {
            var idx = _itemList.IndexOf(item);
            if (idx != -1)
                Select(idx, single);
        }

        public void SetItemDisabled(int idx, bool disabled)
        {
            Unselect(idx);
            var i = _itemList[idx];
            i.Disabled = disabled;
        }

        public void SetItemIcon(int idx, Texture icon)
        {
            _itemList[idx].Icon = icon;
        }

        public void SetItemIconRegion(int idx, UIBox2 region)
        {
            _itemList[idx].IconRegion = region;
        }

        public void SetItemSelectable(int idx, bool selectable)
        {
            _itemList[idx].Selectable = selectable;
        }

        public void SetItemText(int idx, string text)
        {
            _itemList[idx].Text = text;
        }

        public void SetItemTooltip(int idx, string tooltip)
        {
            _itemList[idx].TooltipText = tooltip;
        }

        public void SetItemTooltipEnabled(int idx, bool enabled)
        {
            _itemList[idx].TooltipEnabled = true;
        }

        public void SortItemsByText()
        {
            _itemList.Sort((p, q) => string.Compare(p.Text, q.Text, StringComparison.Ordinal));
        }

        public void Unselect(int idx)
        {
            {
                var i = _itemList[idx];
                if (!i.Selected) return;
                i.Selected = false;
                OnItemDeselected?.Invoke(new ItemListDeselectedEventArgs(idx, this));
            }
        }

        public void Unselect(Item item)
        {
            var idx = _itemList.IndexOf(item);
            if (idx == -1) return;
            Unselect(idx);
        }

        public void ClearSelections()
        {
            foreach (var item in _itemList)
            {
                if (item.Selected)
                {
                    Unselect(item);
                }
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
            _scrollBar.MoveToEnd();
            _isAtBottom = true;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var font = ActualFont;
            var listBg = ActualBackground;
            var iconBg = ActualItemBackground;
            var iconSelectedBg = ActualSelectedItemBackground;
            var iconDisabledBg = ActualDisabledItemBackground;

            var offset = -_scrollBar.Value;

            listBg.Draw(handle, PixelSizeBox);

            foreach (var item in _itemList)
            {
                var bg = iconBg;

                if (item.Disabled)
                    bg = iconDisabledBg;

                if (item.Selected)
                    bg = iconSelectedBg;

                var itemHeight = 0f;
                if (item.Icon != null)
                {
                    itemHeight = item.IconSize.Y;
                }

                itemHeight = Math.Max(itemHeight, ActualFont.GetHeight(UIScale));
                itemHeight += ActualItemBackground.MinimumSize.Y;

                item.Region = UIBox2.FromDimensions(0, offset, PixelWidth, itemHeight);

                bg.Draw(handle, item.Region.Value);

                var contentBox = bg.GetContentBox(item.Region.Value);
                var drawOffset = contentBox.TopLeft;
                if (item.Icon != null)
                {
                    if (item.IconRegion.Size == Vector2.Zero)
                    {
                        handle.DrawTextureRect(item.Icon, UIBox2.FromDimensions(drawOffset, item.Icon.Size), item.IconModulate);
                    }
                    else
                    {
                        handle.DrawTextureRectRegion(item.Icon, UIBox2.FromDimensions(drawOffset, item.Icon.Size), item.IconRegion, item.IconModulate);
                    }
                }

                if (item.Text != null)
                {
                    var textBox = new UIBox2(contentBox.Left + item.IconSize.X, contentBox.Top, contentBox.Right, contentBox.Bottom);
                    DrawTextInternal(handle, item.Text, textBox);
                }

                offset += itemHeight;
            }
        }

        protected void DrawTextInternal(DrawingHandleScreen handle, string text, UIBox2 box)
        {
            var font = ActualFont;

            var color = ActualFontColor;
            var offsetY = (int) (box.Height - font.GetHeight(UIScale)) / 2;
            var baseLine = new Vector2i(0, offsetY + font.GetAscent(UIScale)) + box.TopLeft;

            foreach (var chr in text)
            {
                if (!font.TryGetCharMetrics(chr, UIScale, out var metrics))
                {
                    continue;
                }

                if (!(baseLine.X < box.Left || baseLine.X + metrics.Advance > box.Right))
                {
                    font.DrawChar(handle, chr, baseLine, UIScale, color);
                }

                baseLine += (metrics.Advance, 0);
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            var size = Vector2.Zero;
            if (ActualBackground != null)
            {
                size += ActualBackground.MinimumSize / UIScale;
            }

            return size;
        }

        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (SelectMode == ItemListSelectMode.None || args.Function != EngineKeyFunctions.Use)
            {
                return;
            }

            foreach (var item in _itemList)
            {
                if (item.Region == null)
                    continue;
                if (!item.Region.Value.Contains(args.RelativePosition))
                    continue;
                if (item.Selectable && !item.Disabled)
                    if (item.Selected)
                        Unselect(item);
                    else
                        Select(item, SelectMode == ItemListSelectMode.Single);
                break;
            }
        }

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

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

            if (FloatMath.CloseTo(0, args.Delta.Y))
            {
                return;
            }

            _scrollBar.ValueTarget -= _getScrollSpeed() * args.Delta.Y;
            _isAtBottom = _scrollBar.IsAtEnd;
        }

        [Pure]
        private int _getScrollSpeed()
        {
            var font = ActualFont;
            return font.GetHeight(UIScale) * 2;
        }

        [Pure]
        private UIBox2 _getContentBox()
        {
            var style = ActualBackground;
            return style?.GetContentBox(SizeBox) ?? SizeBox;
        }

        protected override void Resized()
        {
            base.Resized();

            var styleBoxSize = ActualBackground?.MinimumSize.Y ?? 0;

            _scrollBar.Page = PixelSize.Y - styleBoxSize;
            RecalculateContentHeight();
        }

        protected internal override void UIScaleChanged()
        {
             RecalculateContentHeight();

             base.UIScaleChanged();
        }

        private void _updateScrollbarVisibility()
        {
            _scrollBar.Visible = _totalContentHeight + ActualBackground.MinimumSize.Y > PixelHeight;
        }

        public sealed class Item
        {
            public string Text;
            public string TooltipText;
            public Texture Icon;
            public UIBox2 IconRegion;
            public Color IconModulate = Color.White;
            public bool Selected;
            public bool Selectable = true;
            public bool TooltipEnabled = true;
            public bool Disabled;

            public UIBox2? Region;

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
            Multiple
        }
    }
}
