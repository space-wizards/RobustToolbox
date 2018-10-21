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
            get => GameController.OnGodot ? SceneControl.Selected : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Selected = value;
                }
            }
        }

        public void AddItem(Texture icon, string label, int id = 1)
        {
            if (GameController.OnGodot)
            {
                SceneControl.AddIconItem(icon.GodotTexture, label, id);
            }
        }

        public void AddItem(string label, int id = 1)
        {
            if (GameController.OnGodot)
            {
                SceneControl.AddItem(label, id);
            }
        }

        public void AddSeparator()
        {
            if (GameController.OnGodot)
            {
                SceneControl.AddSeparator();
            }
        }

        public void Clear()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Clear();
            }
        }


        public int ItemCount => GameController.OnGodot ? SceneControl.GetItemCount() : default;

        public int GetItemId(int idx)
        {
            return GameController.OnGodot ? SceneControl.GetItemId(idx) : throw new NotImplementedException();
        }

        public object GetItemMetadata(int idx)
        {
            return GameController.OnGodot ? SceneControl.GetItemMetadata(idx) : throw new NotImplementedException();
        }

        public int SelectedId => GameController.OnGodot ? SceneControl.GetSelectedId() : default;

        public object SelectedMetadata =>
            GameController.OnGodot ? SceneControl.GetSceneInstanceLoadPlaceholder() : default;

        public bool IsItemDisabled(int idx)
        {
            return GameController.OnGodot ? SceneControl.IsItemDisabled(idx) : default;
        }

        public void RemoveItem(int idx)
        {
            if (GameController.OnGodot)
            {
                SceneControl.RemoveItem(idx);
            }
        }

        public void Select(int idx)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Select(idx);
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
                SceneControl.SetItemDisabled(idx, disabled);
            }
        }

        public void SetItemIcon(int idx, Texture texture)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemIcon(idx, texture);
            }
        }

        public void SetItemId(int idx, int id)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemId(idx, id);
            }
        }

        public void SetItemMetadata(int idx, object metadata)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemMetadata(idx, metadata);
            }
        }

        public void SetItemText(int idx, string text)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetItemText(idx, text);
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

        new private Godot.OptionButton SceneControl;

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.OptionButton();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.OptionButton) control;
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
