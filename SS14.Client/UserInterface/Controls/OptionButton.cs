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
            get => GameController.OnGodot ? (int)SceneControl.Get("selected") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("selected", value);
                }
            }
        }

        public void AddItem(Texture icon, string label, int id = 1)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("add_icon_item", icon.GodotTexture, label, id);
            }
        }

        public void AddItem(string label, int id = 1)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("add_item", label, id);
            }
        }

        public void AddSeparator()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("add_separator");
            }
        }

        public void Clear()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("clear");
            }
        }

        public int ItemCount => GameController.OnGodot ? (int)SceneControl.Call("get_item_count") : default;

        public int GetItemId(int idx)
        {
            return GameController.OnGodot ? (int)SceneControl.Call("get_item_id", idx) : throw new NotImplementedException();
        }

        public object GetItemMetadata(int idx)
        {
            return GameController.OnGodot ? SceneControl.Call("get_item_metadata", idx) : throw new NotImplementedException();
        }

        public int SelectedId => GameController.OnGodot ? (int)SceneControl.Call("get_selected_id") : default;

        public object SelectedMetadata =>
            GameController.OnGodot ? SceneControl.GetSceneInstanceLoadPlaceholder() : default;

        public bool IsItemDisabled(int idx)
        {
            return GameController.OnGodot ? (bool)SceneControl.Call("is_item_disabled", idx) : default;
        }

        public void RemoveItem(int idx)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("remove_item", idx);
            }
        }

        public void Select(int idx)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("select", idx);
            }
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
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_disabled", idx, disabled);
            }
        }

        public void SetItemIcon(int idx, Texture texture)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_icon", idx, texture);
            }
        }

        public void SetItemId(int idx, int id)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_id", idx, id);
            }
        }

        public void SetItemMetadata(int idx, object metadata)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_metadata", idx, metadata);
            }
        }

        public void SetItemText(int idx, string text)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_text", idx, text);
            }
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
            OnItemSelected?.Invoke(new ItemSelectedEventArgs((int) id, this));
        }
    }
}
