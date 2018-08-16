using System;
using System.Collections.Generic;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Tree))]
    public class Tree : Control
    {
        new Godot.Tree SceneControl;
        readonly Dictionary<Godot.TreeItem, Item> ItemMap = new Dictionary<Godot.TreeItem, Item>();

        public bool HideRoot
        {
            get => SceneControl.HideRoot;
            set => SceneControl.HideRoot = value;
        }

        public int Columns
        {
            get => SceneControl.Columns;
            set => SceneControl.Columns = value;
        }

        public bool ColumnTitlesVisible
        {
            get => SceneControl.AreColumnTitlesVisible();
            set => SceneControl.SetColumnTitlesVisible(value);
        }

        public Item Root => GetItem(SceneControl.GetRoot());
        public Item Selected => GetItem(SceneControl.GetSelected());
        public int SelectedColumn => SceneControl.GetSelectedColumn();

        public event Action OnItemSelected;

        #region Construction
        public Tree(string name) : base(name)
        {
        }
        public Tree() : base()
        {
        }
        public Tree(Godot.Tree panel) : base(panel)
        {
        }

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Tree();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Tree)control;
        }
        #endregion Construction

        public void Clear()
        {
            foreach (var item in ItemMap.Values)
            {
                item.Dispose();
            }

            ItemMap.Clear();
            SceneControl.Clear();
        }

        public Item CreateItem(Item parent = null, int idx = -1)
        {
            var nativeItem = (Godot.TreeItem)SceneControl.CreateItem(parent?.NativeItem);
            var item = new Item(nativeItem, this);
            ItemMap[nativeItem] = item;
            return item;
        }

        public void EnsureCursorIsVisible()
        {
            SceneControl.EnsureCursorIsVisible();
        }

        public int GetColumnAtPosition(Vector2 position)
        {
            return SceneControl.GetColumnAtPosition(position.Convert());
        }

        public string GetColumnTitle(int column)
        {
            return SceneControl.GetColumnTitle(column);
        }

        public void SetColumnTitle(int column, string title)
        {
            SceneControl.SetColumnTitle(column, title);
        }

        public int GetColumnWidth(int column)
        {
            return SceneControl.GetColumnWidth(column);
        }

        public Vector2 GetScroll()
        {
            return SceneControl.GetScroll().Convert();
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
