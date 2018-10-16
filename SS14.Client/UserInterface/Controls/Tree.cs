using System;
using System.Collections.Generic;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap("Tree")]
    public class Tree : Control
    {
        #if GODOT
        new Godot.Tree SceneControl;
        readonly Dictionary<Godot.TreeItem, Item> ItemMap = new Dictionary<Godot.TreeItem, Item>();
        #endif

        public bool HideRoot
        {
            #if GODOT
            get => SceneControl.HideRoot;
            set => SceneControl.HideRoot = value;
            #else
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
            #endif
        }

        public int Columns
        {
            #if GODOT
            get => SceneControl.Columns;
            set => SceneControl.Columns = value;
            #else
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
            #endif
        }

        public bool ColumnTitlesVisible
        {
            #if GODOT
            get => SceneControl.AreColumnTitlesVisible();
            set => SceneControl.SetColumnTitlesVisible(value);
            #else
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
            #endif
        }

        #if GODOT
        public Item Root => GetItem(SceneControl.GetRoot());
        public Item Selected => GetItem(SceneControl.GetSelected());
        public int SelectedColumn => SceneControl.GetSelectedColumn();
        #else
        public Item Root => throw new NotImplementedException();
        public Item Selected => throw new NotImplementedException();
        public int SelectedColumn => throw new NotImplementedException();
        #endif

        public event Action OnItemSelected;

        #region Construction
        public Tree(string name) : base(name)
        {
        }
        public Tree() : base()
        {
        }
        #if GODOT
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
            SceneControl = (Godot.Tree)control;
        }
        #endif
        #endregion Construction

        public void Clear()
        {
            #if GODOT
            foreach (var item in ItemMap.Values)
            {
                item.Dispose();
            }

            ItemMap.Clear();
            SceneControl.Clear();
            #endif
        }

        public Item CreateItem(Item parent = null, int idx = -1)
        {
            #if GODOT
            var nativeItem = (Godot.TreeItem)SceneControl.CreateItem(parent?.NativeItem);
            var item = new Item(nativeItem, this);
            ItemMap[nativeItem] = item;
            return item;
            #else
            throw new NotImplementedException();
            #endif
        }

        public void EnsureCursorIsVisible()
        {
            #if GODOT
            SceneControl.EnsureCursorIsVisible();
            #endif
        }

        public int GetColumnAtPosition(Vector2 position)
        {
            #if GODOT
            return SceneControl.GetColumnAtPosition(position.Convert());
            #else
            throw new NotImplementedException();
            #endif
        }

        public string GetColumnTitle(int column)
        {
            #if GODOT
            return SceneControl.GetColumnTitle(column);
            #else
            throw new NotImplementedException();
            #endif
        }

        public void SetColumnTitle(int column, string title)
        {
            #if GODOT
            SceneControl.SetColumnTitle(column, title);
            #endif
        }

        public int GetColumnWidth(int column)
        {
            #if GODOT
            return SceneControl.GetColumnWidth(column);
            #else
            throw new NotImplementedException();
            #endif
        }

        public Vector2 GetScroll()
        {
            #if GODOT
            return SceneControl.GetScroll().Convert();
            #else
            throw new NotImplementedException();
            #endif
        }

        #if GODOT
        Item GetItem(Godot.TreeItem item)
        {
            if (ItemMap.TryGetValue(item, out var ret))
            {
                return ret;
            }
            return null;
        }
        #endif

        #region Signals
        #if GODOT
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
        #endif
        #endregion Signals

        public sealed class Item : IDisposable
        {
            #if GODOT
            internal Godot.TreeItem NativeItem;
            #endif
            public readonly Tree Parent;

            public object Metadata { get; set; }

            public string GetText(int column)
            {
                #if GODOT
                return NativeItem.GetText(column);
                #else
                throw new NotImplementedException();
                #endif
            }

            public void SetText(int column, string text)
            {
                #if GODOT
                NativeItem.SetText(column, text);
                #endif
            }

            public bool IsSelectable(int column)
            {
                #if GODOT
                return NativeItem.IsSelectable(column);
                #else
                throw new NotImplementedException();
                #endif
            }

            public void SetSelectable(int column, bool selectable)
            {
                #if GODOT
                NativeItem.SetSelectable(column, selectable);
                #else
                throw new NotImplementedException();
                #endif
            }

            #if GODOT
            internal Item(Godot.TreeItem native, Tree parent)
            {
                NativeItem = native;
                Parent = parent;
            }
            #endif
            public void Dispose()
            {
                #if GODOT
                NativeItem?.Dispose();
                NativeItem = null;
                #endif
            }
        }
    }
}
