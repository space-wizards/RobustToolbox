using System;
using System.Collections.Generic;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Tree))]
    public class Tree : Control
    {
        readonly Dictionary<Godot.TreeItem, Item> ItemMap = new Dictionary<Godot.TreeItem, Item>();
        private readonly List<Item> _itemList = new List<Item>();

        public bool HideRoot
        {
            get => GameController.OnGodot ? (bool)SceneControl.Get("hide_root") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("hide_root", value);
                }
            }
        }

        public int Columns
        {
            get => GameController.OnGodot ? (int)SceneControl.Get("columns") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("columns", value);
                }
            }
        }

        public bool ColumnTitlesVisible
        {
            get => GameController.OnGodot ? (bool)SceneControl.Call("are_column_items_visible") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Call("set_column_titles_visible", value);
                }
            }
        }

        public Item Root => GameController.OnGodot ? GetItem((Godot.TreeItem)SceneControl.Call("get_root")) : default;
        public Item Selected => GameController.OnGodot ? GetItem((Godot.TreeItem)SceneControl.Call("get_selected")) : default;
        public int SelectedColumn => GameController.OnGodot ? (int)SceneControl.Call("get_selected_column") : default;

        public event Action OnItemSelected;

        #region Construction

        public Tree(string name) : base(name)
        {
        }

        public Tree() : base()
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
            SceneControl.Call("clear");

            foreach (var item in _itemList)
            {
                item.Dispose();
            }

            _itemList.Clear();
        }

        public Item CreateItem(Item parent = null, int idx = -1)
        {
            if (GameController.OnGodot)
            {
                var nativeItem = (Godot.TreeItem) SceneControl.Call("create_item", parent?.NativeItem);
                var item = new Item(nativeItem, this);
                ItemMap[nativeItem] = item;
                return item;
            }
            else
            {
                var item = new Item(this);
                _itemList.Add(item);
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

        public int GetColumnAtPosition(Vector2 position)
        {
            return GameController.OnGodot ? (int)SceneControl.Call("get_column_at_position", position.Convert()) : default;
        }

        public string GetColumnTitle(int column)
        {
            return GameController.OnGodot ?(string)SceneControl.Call("get_column_title", column) : default;
        }

        public void SetColumnTitle(int column, string title)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_column_title", column, title);
            }
        }

        public int GetColumnWidth(int column)
        {
            return GameController.OnGodot ? (int)SceneControl.Call("get_column_width", column) : default;
        }

        public Vector2 GetScroll()
        {
            return GameController.OnGodot ? ((Godot.Vector2)SceneControl.Call("get_scroll")).Convert() : default;
        }

        Item GetItem(Godot.TreeItem item)
        {
            if (ItemMap.TryGetValue(item, out var ret))
            {
                return ret;
            }

            return null;
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
            internal Godot.TreeItem NativeItem;
            public readonly Tree Parent;
            public object Metadata { get; set; }

            public string GetText(int column)
            {
                return GameController.OnGodot ? NativeItem.GetText(column) : default;
            }

            public void SetText(int column, string text)
            {
                if (GameController.OnGodot)
                {
                    NativeItem.SetText(column, text);
                }
            }

            public bool IsSelectable(int column)
            {
                return GameController.OnGodot ? NativeItem.IsSelectable(column) : default;
            }

            public void SetSelectable(int column, bool selectable)
            {
                if (GameController.OnGodot)
                {
                    NativeItem.SetSelectable(column, selectable);
                }
            }

            internal Item(Tree parent)
            {
                Parent = parent;
            }

            internal Item(Godot.TreeItem native, Tree parent)
            {
                NativeItem = native;
                Parent = parent;
            }

            public void Dispose()
            {
                if (GameController.OnGodot)
                {
                    NativeItem?.Dispose();
                }
            }
        }
    }
}
