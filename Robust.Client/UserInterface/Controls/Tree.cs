using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class Tree : Control
    {
        public const string StylePropertyItemBoxSelected = "item-selected";
        public const string StylePropertyBackground = "background";

        private readonly List<Item> _itemList = new();

        private Item? _root;
        private int? _selectedIndex;

        private VScrollBar _scrollBar;

        public bool HideRoot { get; set; }

        public Item? Root => _root;

        public Item? Selected => _selectedIndex == null ? null : _itemList[_selectedIndex.Value];

        public event Action? OnItemSelected;

        public Tree()
        {
            MouseFilter = MouseFilterMode.Pass;
            RectClipContent = true;

            _scrollBar = new VScrollBar
            {
                Name = "_v_scroll",
                VerticalAlignment = VAlignment.Stretch,
                HorizontalAlignment = HAlignment.Right
            };
            AddChild(_scrollBar);
        }

        public void Clear()
        {
            foreach (var item in _itemList)
            {
                item.Dispose();
            }

            _itemList.Clear();
            _selectedIndex = null;
            _root = null;
            _updateScrollbar();
        }

        public Item CreateItem(Item? parent = null, int idx = -1)
        {
            if (parent != null)
            {
                if (parent.Parent != this)
                {
                    throw new ArgumentException("Parent must be owned by this tree.", nameof(parent));
                }

                if (parent.Disposed)
                {
                    throw new ArgumentException("Parent is disposed", nameof(parent));
                }
            }

            var item = new Item(this, _itemList.Count);
            _itemList.Add(item);

            if (_root == null)
            {
                _root = item;
            }
            else
            {
                parent = parent ?? _root;
                parent.Children.Add(item);
            }

            _updateScrollbar();
            return item;
        }

        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            var item = _tryFindItemAtPosition(args.RelativePosition);

            if (item != null && item.Selectable)
            {
                _selectedIndex = item.Index;
                OnItemSelected?.Invoke();
                args.Handle();
            }
        }

        private Item? _tryFindItemAtPosition(Vector2 position)
        {
            var font = _getFont();
            if (font == null || _root == null)
            {
                return null;
            }

            var background = _getBackground();
            if (background != null)
            {
                position -= background.GetContentOffset(Vector2.Zero);
            }

            var vOffset = -_scrollBar.Value;
            Item? final = null;

            bool DoSearch(Item item, float hOffset)
            {
                var itemHeight = font.GetHeight(UIScale);
                var itemBox = UIBox2.FromDimensions((hOffset, vOffset), (PixelWidth - hOffset, itemHeight));
                if (itemBox.Contains(position))
                {
                    final = item;
                    return true;
                }

                vOffset += itemHeight;
                hOffset += 5;

                foreach (var child in item.Children)
                {
                    if (DoSearch(child, hOffset))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (HideRoot)
            {
                foreach (var child in _root.Children)
                {
                    if (DoSearch(child, 0))
                    {
                        break;
                    }
                }
            }
            else
            {
                DoSearch(_root, 0);
            }

            return final;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var font = _getFont();
            if (font == null || _root == null)
            {
                return;
            }

            var background = _getBackground();
            var itemSelected = _getItemSelectedStyle();
            var vOffset = -_scrollBar.Value;
            var hOffset = 0f;

            if (itemSelected == null)
            {
                return;
            }

            if (background != null)
            {
                background.Draw(handle, PixelSizeBox);
                var (bho, bvo) = background.GetContentOffset(Vector2.Zero);
                vOffset += bvo;
                hOffset += bho;
            }

            if (HideRoot)
            {
                foreach (var child in _root.Children)
                {
                    _drawItem(handle, ref vOffset, hOffset, child, font, itemSelected);
                }
            }
            else
            {
                _drawItem(handle, ref vOffset, hOffset, _root, font, itemSelected);
            }
        }

        private void _drawItem(
            DrawingHandleScreen handle, ref float vOffset, float hOffset, Item item,
            Font font, StyleBox itemSelected)
        {
            var itemHeight = font.GetHeight(UIScale) + itemSelected.MinimumSize.Y;
            var selected = item.Index == _selectedIndex;
            if (selected)
            {
                itemSelected.Draw(handle, UIBox2.FromDimensions(hOffset, vOffset, PixelWidth - hOffset, itemHeight));
            }

            if (!string.IsNullOrWhiteSpace(item.Text))
            {
                var offset = itemSelected.GetContentOffset(Vector2.Zero);
                var baseLine = offset + (hOffset, vOffset + font.GetAscent(UIScale));
                foreach (var rune in item.Text.EnumerateRunes())
                {
                    baseLine += (font.DrawChar(handle, rune, baseLine, UIScale, Color.White), 0);
                }
            }

            vOffset += itemHeight;
            hOffset += 5;
            foreach (var child in item.Children)
            {
                _drawItem(handle, ref vOffset, hOffset, child, font, itemSelected);
            }
        }

        private float _getInternalHeight()
        {
            var font = _getFont();
            if (font == null || _root == null)
            {
                return 0;
            }

            if (HideRoot)
            {
                var sum = 0f;
                foreach (var child in _root.Children)
                {
                    sum += _getItemHeight(child, font);
                }

                return sum;
            }

            return _getItemHeight(_root, font);
        }

        private float _getItemHeight(Item item, Font font)
        {
            float sum = font.GetHeight(UIScale);

            foreach (var child in item.Children)
            {
                sum += _getItemHeight(child, font);
            }

            return sum;
        }

        private void _updateScrollbar()
        {
            var internalHeight = _getInternalHeight();
            _scrollBar.MaxValue = Math.Max(internalHeight, PixelHeight);
            _scrollBar.Page = PixelHeight;
            _scrollBar.Visible = internalHeight > PixelHeight;
        }

        protected override void Resized()
        {
            base.Resized();

            _updateScrollbar();
        }

        private Font? _getFont()
        {
            if (TryGetStyleProperty<Font>("font", out var font))
            {
                return font;
            }

            return null;
        }

        private StyleBox? _getBackground()
        {
            if (TryGetStyleProperty<StyleBox>(StylePropertyBackground, out var box))
            {
                return box;
            }

            return null;
        }

        private StyleBox? _getItemSelectedStyle()
        {
            if (TryGetStyleProperty<StyleBox>(StylePropertyItemBoxSelected, out var box))
            {
                return box;
            }

            return null;
        }

        protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
        {
            base.MouseWheel(args);

            _scrollBar.ValueTarget -= args.Delta.Y * 50;
        }

        public sealed class Item : IDisposable
        {
            internal readonly List<Item> Children = new();
            internal readonly int Index;
            public readonly Tree Parent;
            public object? Metadata { get; set; }
            public bool Disposed { get; private set; }

            public string? Text { get; set; }

            public bool Selectable { get; set; } = true;

            internal Item(Tree parent, int index)
            {
                Parent = parent;
                Index = index;
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
