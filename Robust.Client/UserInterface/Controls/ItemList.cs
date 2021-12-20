using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Robust.Client.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Timer = Robust.Shared.Timing.Timer;

namespace Robust.Client.UserInterface.Controls
{
    public class ItemList : Control, IList<ItemList.Item>
    {
        private bool _isAtBottom = true;
        private int _totalContentHeight;

        private VScrollBar _scrollBar;
        private readonly List<Item> _itemList = new();
        public event Action<ItemListSelectedEventArgs>? OnItemSelected;
        public event Action<ItemListDeselectedEventArgs>? OnItemDeselected;
        public event Action<ItemListHoverEventArgs>? OnItemHover;

        public const string StylePropertyBackground = "itemlist-background";
        public const string StylePropertyItemBackground = "item-background";
        public const string StylePropertySelectedItemBackground = "selected-item-background";
        public const string StylePropertyDisabledItemBackground = "disabled-item-background";

        public int Count => _itemList.Count;
        public bool IsReadOnly => false;

        public bool ScrollFollowing { get; set; } = false;
        public int ButtonDeselectDelay { get; set; } = 100;

        public ItemListSelectMode SelectMode { get; set; } = ItemListSelectMode.Single;

        public ItemList()
        {
            MouseFilter = MouseFilterMode.Pass;

            RectClipContent = true;

            _scrollBar = new VScrollBar
            {
                Name = "_v_scroll",

                HorizontalAlignment = HAlignment.Right
            };
            AddChild(_scrollBar);
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

        public void Add(Item item)
        {
            if (item == null) return;
            if(item.Owner != this) throw new ArgumentException("Item is owned by another ItemList!");

            _itemList.Add(item);

            item.OnSelected += Select;
            item.OnDeselected += Deselect;

            RecalculateContentHeight();
            if (_isAtBottom && ScrollFollowing)
                _scrollBar.MoveToEnd();
        }

        public Item AddItem(string text, Texture? icon = null, bool selectable = true)
        {
            var item = new Item(this) {Text = text, Icon = icon, Selectable = selectable};
            Add(item);
            return item;
        }

        public void Clear()
        {
            foreach (var item in _itemList.ToArray())
            {
                Remove(item);
            }

            _totalContentHeight = 0;
        }

        public bool Contains(Item item)
        {
            return _itemList.Contains(item);
        }

        public void CopyTo(Item[] array, int arrayIndex)
        {
            _itemList.CopyTo(array, arrayIndex);
        }

        public bool Remove(Item item)
        {
            if (item == null) return false;

            var value =  _itemList.Remove(item);

            item.OnSelected -= Select;
            item.OnDeselected -= Deselect;

            RecalculateContentHeight();
            if (_isAtBottom && ScrollFollowing)
                _scrollBar.MoveToEnd();

            return value;
        }

        public void RemoveAt(int index)
        {
            Remove(this[index]);
        }

        public IEnumerator<Item> GetEnumerator()
        {
            return _itemList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(Item item)
        {
            return _itemList.IndexOf(item);
        }

        public void Insert(int index, Item item)
        {
            if (item == null) return;
            if(item.Owner != this) throw new ArgumentException("Item is owned by another ItemList!");

            _itemList.Insert(index, item);

            item.OnSelected += Select;
            item.OnDeselected += Deselect;

            RecalculateContentHeight();
            if (_isAtBottom && ScrollFollowing)
                _scrollBar.MoveToEnd();
        }

        // Without this attribute, this would compile into a property called "Item", causing problems with the Item class.
        [System.Runtime.CompilerServices.IndexerName("IndexItem")]
        public Item this[int index]
        {
            get => _itemList[index];
            set => _itemList[index] = value;
        }

        public IEnumerable<Item> GetSelected()
        {
            var list = new List<Item>();

            for (var i = 0; i < _itemList.Count; i++)
            {
                var item = _itemList[i];
                if (item.Selected) list.Add(item);
            }

            return list;
        }

        private void Select(int idx)
        {
            OnItemSelected?.Invoke(new ItemListSelectedEventArgs(idx, this));
        }

        private void Select(Item item)
        {
            var idx = IndexOf(item);
            if (idx != -1)
                Select(idx);
        }

        private void Deselect(int idx)
        {
            OnItemDeselected?.Invoke(new ItemListDeselectedEventArgs(idx, this));
        }

        private void Deselect(Item item)
        {
            var idx = IndexOf(item);
            if (idx == -1) return;
            Deselect(idx);
        }

        public void ClearSelected()
        {
            foreach (var item in GetSelected())
            {
                item.Selected = false;
            }
        }

        public void SortItemsByText()
        {
            _itemList.Sort((p, q) => string.Compare(p.Text, q.Text, StringComparison.Ordinal));
        }

        public void EnsureCurrentIsVisible()
        {
            // TODO: Implement this.
        }

        public int GetItemAtPosition(Vector2 position, bool exact = false)
        {
            throw new NotImplementedException();
        }

        public Font ActualFont
        {
            get
            {
                if (TryGetStyleProperty<Font>("font", out var font))
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
                if (TryGetStyleProperty<StyleBox>(StylePropertyBackground, out var bg))
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
                if (TryGetStyleProperty<StyleBox>(StylePropertyItemBackground, out var bg))
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
                if (TryGetStyleProperty<StyleBox>(StylePropertySelectedItemBackground, out var bg))
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
                if (TryGetStyleProperty<StyleBox>(StylePropertyDisabledItemBackground, out var bg))
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

            var sizeBox = PixelSizeBox;

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
                {
                    bg = iconSelectedBg;
                }

                var itemHeight = 0f;
                if (item.Icon != null)
                {
                    itemHeight = item.IconSize.Y;
                }

                itemHeight = Math.Max(itemHeight, font.GetHeight(UIScale));
                itemHeight += ActualItemBackground.MinimumSize.Y;

                var region = UIBox2.FromDimensions(0, offset, PixelWidth, itemHeight);
                item.Region = region;

                if (region.Intersects(sizeBox))
                {
                    bg.Draw(handle, item.Region.Value);

                    var contentBox = bg.GetContentBox(item.Region.Value);
                    var drawOffset = contentBox.TopLeft;
                    if (item.Icon != null)
                    {
                        if (item.IconRegion.Size == Vector2.Zero)
                        {
                            handle.DrawTextureRect(item.Icon, UIBox2.FromDimensions(drawOffset, item.Icon.Size),
                                item.IconModulate);
                        }
                        else
                        {
                            handle.DrawTextureRectRegion(item.Icon, UIBox2.FromDimensions(drawOffset, item.Icon.Size),
                                item.IconRegion, item.IconModulate);
                        }
                    }

                    if (item.Text != null)
                    {
                        var textBox = new UIBox2(contentBox.Left + item.IconSize.X, contentBox.Top, contentBox.Right,
                            contentBox.Bottom);
                        DrawTextInternal(handle, item.Text, textBox);
                    }
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

            foreach (var rune in text.EnumerateRunes())
            {
                if (!font.TryGetCharMetrics(rune, UIScale, out var metrics))
                {
                    continue;
                }

                if (!(baseLine.X < box.Left || baseLine.X + metrics.Advance > box.Right))
                {
                    font.DrawChar(handle, rune, baseLine, UIScale, color);
                }

                baseLine += (metrics.Advance, 0);
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
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

            if (SelectMode == ItemListSelectMode.None || args.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            foreach (var item in _itemList)
            {
                if (item.Region == null)
                    continue;

                if (!item.Region.Value.Contains(args.RelativePixelPosition))
                    continue;

                if (item.Selectable && !item.Disabled)
                {
                    if (item.Selected && SelectMode != ItemListSelectMode.Button)
                    {
                        ClearSelected();
                        item.Selected = false;
                        return;
                    }

                    if(SelectMode != ItemListSelectMode.Multiple)
                        ClearSelected();
                    item.Selected = true;
                    if (SelectMode == ItemListSelectMode.Button)
                        Timer.Spawn(ButtonDeselectDelay, () => {  item.Selected = false; } );
                }
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

            if (MathHelper.CloseToPercent(0, args.Delta.Y))
            {
                return;
            }

            _scrollBar.ValueTarget -= _getScrollSpeed() * args.Delta.Y;
            _isAtBottom = _scrollBar.IsAtEnd;

            args.Handle();
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

        public enum ItemListSelectMode : byte
        {
            None,
            Single,
            Multiple,
            Button,
        }

        public sealed class Item
        {
            public event Action<Item>? OnSelected;
            public event Action<Item>? OnDeselected;

            private bool _selected = false;
            private bool _disabled = false;

            public ItemList Owner { get; }
            public string? Text { get; set; }
            public string? TooltipText { get; set; }
            public Texture? Icon { get; set; }
            public UIBox2 IconRegion { get; set; }
            public Color IconModulate { get; set; } = Color.White;
            public bool Selectable { get; set; } = true;
            public bool TooltipEnabled { get; set; } = true;
            public UIBox2? Region { get; set; }
            public object? Metadata { get; set; }

            public bool Disabled
            {
                get => _disabled;
                set
                {
                    _disabled = value;
                    if (Selected) Selected = false;
                }
            }
            public bool Selected
            {
                get => _selected;
                set
                {
                    if (!Selectable) return;
                    _selected = value;
                    if(_selected) OnSelected?.Invoke(this);
                    else OnDeselected?.Invoke(this);
                }
            }

            public Vector2 IconSize
            {
                get
                {
                    if (Icon == null)
                        return Vector2.Zero;
                    return IconRegion.Size != Vector2.Zero ? IconRegion.Size : Icon.Size;
                }
            }

            public Item(ItemList owner)
            {
                Owner = owner;
            }
        }
    }
}
