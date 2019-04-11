using System;
using System.Collections.Generic;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Utility;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Tree))]
    public class Tree : Control
    {
        public const string StylePropertyItemBoxSelected = "item-selected";
        public const string StylePropertyBackground = "background";

        readonly Dictionary<Godot.TreeItem, Item> ItemMap = new Dictionary<Godot.TreeItem, Item>();
        private readonly List<Item> _itemList = new List<Item>();

        private bool _hideRoot;

        private Item _root;
        private int? _selectedIndex;

        private VScrollBar _scrollBar;

        public bool HideRoot
        {
            get => GameController.OnGodot ? (bool) SceneControl.Get("hide_root") : _hideRoot;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("hide_root", value);
                }
                else
                {
                    _hideRoot = value;
                }
            }
        }

        public Item Root => GameController.OnGodot ? GetItem((Godot.TreeItem) SceneControl.Call("get_root")) : _root;

        public Item Selected
        {
            get
            {
                if (GameController.OnGodot)
                {
                    return GetItem((Godot.TreeItem) SceneControl.Call("get_selected"));
                }

                return _selectedIndex == null ? null : _itemList[_selectedIndex.Value];
            }
        }

        public event Action OnItemSelected;

        #region Construction

        public Tree(string name) : base(name)
        {
        }

        public Tree()
        {
        }

        internal Tree(Godot.Tree panel) : base(panel)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Tree();
        }

        #endregion Construction

        public void Clear()
        {
            foreach (var item in ItemMap.Values)
            {
                item.Dispose();
            }

            ItemMap.Clear();
            if (GameController.OnGodot)
            {
                SceneControl.Call("clear");
            }

            foreach (var item in _itemList)
            {
                item.Dispose();
            }

            _itemList.Clear();
            _selectedIndex = null;
            _root = null;
            _updateScrollbar();
        }

        public Item CreateItem(Item parent = null, int idx = -1)
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

            if (GameController.OnGodot)
            {
                var nativeItem = (Godot.TreeItem) SceneControl.Call("create_item", parent?.NativeItem);
                var item = new Item(nativeItem, this, ItemMap.Count);
                ItemMap[nativeItem] = item;
                return item;
            }
            else
            {
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
        }

        public void EnsureCursorIsVisible()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("ensure_cursor_is_visible");
            }
        }

        public Vector2 GetScroll()
        {
            return GameController.OnGodot ? ((Godot.Vector2) SceneControl.Call("get_scroll")).Convert() : default;
        }

        private Item GetItem(Godot.TreeItem item)
        {
            if (ItemMap.TryGetValue(item, out var ret))
            {
                return ret;
            }

            return null;
        }

        protected override void SetDefaults()
        {
            base.SetDefaults();

            RectClipContent = true;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (GameController.OnGodot)
            {
                return;
            }

            _scrollBar = new VScrollBar {Name = "_v_scroll"};
            AddChild(_scrollBar);
            _scrollBar.SetAnchorAndMarginPreset(LayoutPreset.RightWide);
        }

        protected internal override void MouseDown(GUIMouseButtonEventArgs args)
        {
            base.MouseDown(args);

            if (GameController.OnGodot)
            {
                return;
            }

            var item = _tryFindItemAtPosition(args.RelativePosition);

            if (item != null && item.Selectable)
            {
                _selectedIndex = item.Index;
                OnItemSelected?.Invoke();
            }
        }

        private Item _tryFindItemAtPosition(Vector2 position)
        {
            DebugTools.Assert(!GameController.OnGodot);

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
            Item final = null;

            bool DoSearch(Item item, float hOffset)
            {
                var itemHeight = font.Height;
                var itemBox = UIBox2.FromDimensions((hOffset, vOffset), (Width - hOffset, itemHeight));
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

            if (GameController.OnGodot)
            {
                return;
            }

            var font = _getFont();
            if (font == null || _root == null)
            {
                return;
            }

            var background = _getBackground();
            var itemSelected = _getItemSelectedStyle();
            var vOffset = -_scrollBar.Value;
            var hOffset = 0f;
            if (background != null)
            {
                background.Draw(handle, SizeBox);
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
            var itemHeight = font.Height + itemSelected.MinimumSize.Y;
            var selected = item.Index == _selectedIndex;
            if (selected)
            {
                itemSelected.Draw(handle, UIBox2.FromDimensions(hOffset, vOffset, Width - hOffset, itemHeight));
            }

            if (!string.IsNullOrWhiteSpace(item.Text))
            {
                var offset = itemSelected.GetContentOffset(Vector2.Zero);
                var baseLine = offset + (hOffset, vOffset + font.Ascent);
                foreach (var chr in item.Text)
                {
                    baseLine += (font.DrawChar(handle, chr, baseLine, Color.White), 0);
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
            else
            {
                return _getItemHeight(_root, font);
            }
        }

        private float _getItemHeight(Item item, Font font)
        {
            float sum = font.Height;

            foreach (var child in item.Children)
            {
                sum += _getItemHeight(child, font);
            }

            return sum;
        }

        private void _updateScrollbar()
        {
            if (GameController.OnGodot)
            {
                return;
            }

            var internalHeight = _getInternalHeight();
            _scrollBar.MaxValue = internalHeight;
            _scrollBar.Page = Height;
            _scrollBar.Visible = internalHeight > Height;
        }

        protected override void Resized()
        {
            base.Resized();

            _updateScrollbar();
        }

        private Font _getFont()
        {
            if (TryGetStyleProperty("font", out Font font))
            {
                return font;
            }

            return null;
        }

        private StyleBox _getBackground()
        {
            if (TryGetStyleProperty(StylePropertyBackground, out StyleBox box))
            {
                return box;
            }

            return null;
        }

        private StyleBox _getItemSelectedStyle()
        {
            if (TryGetStyleProperty(StylePropertyItemBoxSelected, out StyleBox box))
            {
                return box;
            }

            return null;
        }

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            base.SetGodotProperty(property, value, context);

            if (property == "hide_root")
            {
                HideRoot = (bool) value;
            }
        }

        #region Signals

        private GodotGlue.GodotSignalSubscriber0 __itemSelectedSubscriber;

        protected override void SetupSignalHooks()
        {
            base.SetupSignalHooks();

            __itemSelectedSubscriber = new GodotGlue.GodotSignalSubscriber0();
            __itemSelectedSubscriber.Connect(SceneControl, "item_selected");
            __itemSelectedSubscriber.Signal += () => OnItemSelected?.Invoke();
        }

        protected override void DisposeSignalHooks()
        {
            base.DisposeSignalHooks();

            if (__itemSelectedSubscriber != null)
            {
                __itemSelectedSubscriber?.Disconnect(SceneControl, "item_selected");
                __itemSelectedSubscriber?.Dispose();
                __itemSelectedSubscriber = null;
            }
        }

        #endregion Signals

        public sealed class Item : IDisposable
        {
            internal readonly Godot.TreeItem NativeItem;
            internal readonly List<Item> Children = new List<Item>();
            internal readonly int Index;
            public readonly Tree Parent;
            public object Metadata { get; set; }
            public bool Disposed { get; private set; }

            private string _text;
            private bool _selectable = true;

            public string Text
            {
                get => GameController.OnGodot ? NativeItem.GetText(0) : _text;
                set
                {
                    if (GameController.OnGodot)
                    {
                        NativeItem.SetText(0, value);
                    }
                    else
                    {
                        _text = value;
                    }
                }
            }

            [Obsolete("Use Text")]
            public void SetText(int column, string text)
            {
                Text = text;
            }

            public bool Selectable
            {
                get => GameController.OnGodot ? NativeItem.IsSelectable(0) : _selectable;
                set
                {
                    if (GameController.OnGodot)
                    {
                        NativeItem.SetSelectable(0, value);
                    }
                    else
                    {
                        _selectable = value;
                    }
                }
            }

            [Obsolete("Use Selectable")]
            public void SetSelectable(int column, bool selectable)
            {
                Selectable = selectable;
            }

            internal Item(Tree parent, int index)
            {
                Parent = parent;
                Index = index;
            }

            internal Item(Godot.TreeItem native, Tree parent, int index)
            {
                NativeItem = native;
                Parent = parent;
                Index = index;
            }

            public void Dispose()
            {
                if (GameController.OnGodot)
                {
                    NativeItem?.Dispose();
                }

                Disposed = true;
            }
        }
    }
}
