using System;
using SS14.Client.GodotGlue;
using SS14.Client.Graphics;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.OptionButton))]
    public class OptionButton : Button
    {
        public event Action<ItemSelectedEventArgs> OnItemSelected;

        public int Selected
        {
            get => (int)SceneControl.Get("selected");
            set => SceneControl.Set("selected", value);
        }

        public void AddItem(Texture icon, string label, int id = 1)
        {
            SceneControl.Call("add_icon_item", icon.GodotTexture, label, id);
        }

        public void AddItem(string label, int id = 1)
        {
            SceneControl.Call("add_item", label, id);
        }

        public void AddSeparator()
        {
            SceneControl.Call("add_separator");
        }

        public void Clear()
        {
            SceneControl.Call("clear");
        }

        public int ItemCount => (int)SceneControl.Call("get_item_count");

        public int GetItemId(int idx)
        {
            return (int)SceneControl.Call("get_item_id", idx);
        }

        public object GetItemMetadata(int idx)
        {
            return SceneControl.Call("get_item_metadata", idx);
        }

        public int SelectedId => (int)SceneControl.Call("get_selected_id");

        public object SelectedMetadata => SceneControl.GetSceneInstanceLoadPlaceholder();

        public bool IsItemDisabled(int idx)
        {
            return (bool)SceneControl.Call("is_item_disabled", idx);
        }

        public void RemoveItem(int idx)
        {
            SceneControl.Call("remove_item", idx);
        }

        public void Select(int idx)
        {
            SceneControl.Call("select", idx);
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
            SceneControl.Call("set_item_disabled", idx, disabled);
        }

        public void SetItemIcon(int idx, Texture texture)
        {
            SceneControl.Call("set_item_icon", idx, texture);
        }

        public void SetItemId(int idx, int id)
        {
            SceneControl.Call("set_item_id", idx, id);
        }

        public void SetItemMetadata(int idx, object metadata)
        {
            SceneControl.Call("set_item_metadata", idx, metadata);
        }

        public void SetItemText(int idx, string text)
        {
            SceneControl.Call("set_item_text", idx, text);
        }

        public OptionButton() : base()
        {
        }
        public OptionButton(string name) : base(name)
        {
        }
        internal OptionButton(Godot.OptionButton button) : base(button)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.OptionButton();
        }

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
    }
}
