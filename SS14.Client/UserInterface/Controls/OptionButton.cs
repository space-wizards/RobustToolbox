using System;
#if GODOT
using SS14.Client.GodotGlue;
#endif
using SS14.Client.Graphics;

namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.OptionButton))]
    #endif
    public class OptionButton : Button
    {
        public event Action<ItemSelectedEventArgs> OnItemSelected;

        public int Selected
        {
            #if GODOT
            get => SceneControl.Selected;
            set => SceneControl.Selected = value;
            #else
            get => default;
            set { }
            #endif
        }

        public void AddItem(Texture icon, string label, int id = 1)
        {
            #if GODOT
            SceneControl.AddIconItem(icon.GodotTexture, label, id);
            #endif
        }

        public void AddItem(string label, int id = 1)
        {
            #if GODOT
            SceneControl.AddItem(label, id);
            #endif
        }

        public void AddSeparator()
        {
            #if GODOT
            SceneControl.AddSeparator();
            #endif
        }

        public void Clear()
        {
            #if GODOT
            SceneControl.Clear();
            #endif
        }

        #if GODOT
        public int ItemCount => SceneControl.GetItemCount();
        #else
        public int ItemCount => throw new NotImplementedException();
        #endif

        public int GetItemId(int idx)
        {
            #if GODOT
            return SceneControl.GetItemId(idx);
            #else
            throw new NotImplementedException();
            #endif
        }

        public object GetItemMetadata(int idx)
        {
            #if GODOT
            return SceneControl.GetItemMetadata(idx);
            #else
            throw new NotImplementedException();
            #endif
        }

        #if GODOT
        public int SelectedId => SceneControl.GetSelectedId();
        public object SelectedMetadata => SceneControl.GetSceneInstanceLoadPlaceholder();
        #else
        public int SelectedId => throw new NotImplementedException();
        public object SelectedMetadata => throw new NotImplementedException();
        #endif

        public bool IsItemDisabled(int idx)
        {
            #if GODOT
            return SceneControl.IsItemDisabled(idx);
            #else
            throw new NotImplementedException();
            #endif
        }

        public void RemoveItem(int idx)
        {
            #if GODOT
            SceneControl.RemoveItem(idx);
            #endif
        }

        public void Select(int idx)
        {
            #if GODOT
            SceneControl.Select(idx);
            #endif
        }

        public void SelectId(int id)
        {
            Select(GetIdx(id));
        }

        public int GetIdx(int id)
        {
            for (var i = 0; i < ItemCount; i++)
            {
                if (id == GetItemId(i))
                {
                    return i;
                }
            }

            throw new ArgumentException("ID does not exist.");
        }

        public void SetItemDisabled(int idx, bool disabled)
        {
            #if GODOT
            SceneControl.SetItemDisabled(idx, disabled);
            #endif
        }

        public void SetItemIcon(int idx, Texture texture)
        {
            #if GODOT
            SceneControl.SetItemIcon(idx, texture);
            #endif
        }

        public void SetItemId(int idx, int id)
        {
            #if GODOT
            SceneControl.SetItemId(idx, id);
            #endif
        }

        public void SetItemMetadata(int idx, object metadata)
        {
            #if GODOT
            SceneControl.SetItemMetadata(idx, metadata);
            #endif
        }

        public void SetItemText(int idx, string text)
        {
            #if GODOT
            SceneControl.SetItemText(idx, text);
            #endif
        }

        public OptionButton() : base()
        {
        }
        public OptionButton(string name) : base(name)
        {
        }

        #if GODOT
        internal OptionButton(Godot.OptionButton button) : base(button)
        {
        }

        new private Godot.OptionButton SceneControl;

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.OptionButton();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.OptionButton)control;
        }
        #endif

        public class ItemSelectedEventArgs : EventArgs
        {
            public OptionButton Button { get; }

            /// <summary>
            ///     The ID of the item that has been selected.
            /// </summary>
            public int Id { get; }

            public ItemSelectedEventArgs(int id, OptionButton button)
            {
                Id = id;
                Button = button;
            }
        }

        #if GODOT
        private GodotSignalSubscriber1 __itemSelectedSubscriber;

        protected override void SetupSignalHooks()
        {
            base.SetupSignalHooks();

            __itemSelectedSubscriber = new GodotSignalSubscriber1();
            __itemSelectedSubscriber.Connect(SceneControl, "item_selected");
            __itemSelectedSubscriber.Signal += __itemSelectedHook;
        }

        protected override void DisposeSignalHooks()
        {
            base.DisposeSignalHooks();

            if (__itemSelectedSubscriber != null)
            {
                __itemSelectedSubscriber.Disconnect(SceneControl, "item_selected");
                __itemSelectedSubscriber.Dispose();
                __itemSelectedSubscriber = null;
            }
        }

        private void __itemSelectedHook(object id)
        {
            OnItemSelected?.Invoke(new ItemSelectedEventArgs((int)id, this));
        }
        #endif
    }
}
