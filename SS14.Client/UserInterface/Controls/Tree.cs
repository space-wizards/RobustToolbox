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
        private readonly List<Item> _itemList = new List<Item>();

        public bool HideRoot
        {
            get => GameController.OnGodot ? SceneControl.HideRoot : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.HideRoot = value;
                }
            }
        }

        public int Columns
        {
            get => GameController.OnGodot ? SceneControl.Columns : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Columns = value;
                }
            }
        }

        public bool ColumnTitlesVisible
        {
            get => GameController.OnGodot ? SceneControl.AreColumnTitlesVisible() : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SetColumnTitlesVisible(value);
                }
            }
        }

        public Item Root => GameController.OnGodot ? GetItem(SceneControl.GetRoot()) : default;
        public Item Selected => GameController.OnGodot ? GetItem(SceneControl.GetSelected()) : default;
        public int SelectedColumn => GameController.OnGodot ? SceneControl.GetSelectedColumn() : default;

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

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Tree) control;
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
                var nativeItem = (Godot.TreeItem) SceneControl.CreateItem(parent?.NativeItem);
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
                SceneControl.EnsureCursorIsVisible();
            }
        }

        public int GetColumnAtPosition(Vector2 position)
        {
            return GameController.OnGodot ? SceneControl.GetColumnAtPosition(position.Convert()) : default;
        }

        public string GetColumnTitle(int column)
        {
            return GameController.OnGodot ? SceneControl.GetColumnTitle(column) : default;
        }

        public void SetColumnTitle(int column, string title)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetColumnTitle(column, title);
            }
        }

        public int GetColumnWidth(int column)
        {
            return GameController.OnGodot ? SceneControl.GetColumnWidth(column) : default;
        }

        public Vector2 GetScroll()
        {
            return GameController.OnGodot ? SceneControl.GetScroll().Convert() : default;
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
