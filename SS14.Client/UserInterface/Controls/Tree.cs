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

        public bool HideRoot
        {
            get => (bool)SceneControl.Get("hide_root");
            set => SceneControl.Set("hide_root", value);
        }

        public int Columns
        {
            get => (int)SceneControl.Get("columns");
            set => SceneControl.Set("columns", value);
        }

        public bool ColumnTitlesVisible
        {
            get => (bool)SceneControl.Call("are_column_items_visible");
            set => SceneControl.Call("set_column_titles_visible", value);
        }

        public Item Root => GetItem((Godot.TreeItem)SceneControl.Call("get_root"));
        public Item Selected => GetItem((Godot.TreeItem)SceneControl.Call("get_selected"));
        public int SelectedColumn => (int)SceneControl.Call("get_selected_column");

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
        }

        public Item CreateItem(Item parent = null, int idx = -1)
        {
            var nativeItem = (Godot.TreeItem)SceneControl.Call("create_item", parent?.NativeItem);
            var item = new Item(nativeItem, this);
            ItemMap[nativeItem] = item;
            return item;
        }

        public void EnsureCursorIsVisible()
        {
            SceneControl.Call("ensure_cursor_is_visible");
        }

        public int GetColumnAtPosition(Vector2 position)
        {
            return (int)SceneControl.Call("get_column_at_position", position.Convert());
        }

        public string GetColumnTitle(int column)
        {
            return (string)SceneControl.Call("get_column_title", column);
        }

        public void SetColumnTitle(int column, string title)
        {
            SceneControl.Call("set_column_title", column, title);
        }

        public int GetColumnWidth(int column)
        {
            return (int)SceneControl.Call("get_column_width", column);
        }

        public Vector2 GetScroll()
        {
            return ((Godot.Vector2)SceneControl.Call("get_scroll")).Convert();
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
                return NativeItem.GetText(column);
            }

            public void SetText(int column, string text)
            {
                NativeItem.SetText(column, text);
            }

            public bool IsSelectable(int column)
            {
                return NativeItem.IsSelectable(column);
            }

            public void SetSelectable(int column, bool selectable)
            {
                NativeItem.SetSelectable(column, selectable);
            }

            internal Item(Godot.TreeItem native, Tree parent)
            {
                NativeItem = native;
                Parent = parent;
            }
            public void Dispose()
            {
                NativeItem?.Dispose();
                NativeItem = null;
            }
        }
    }
}
