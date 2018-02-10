using System;
using SS14.Client.GodotGlue;
using SS14.Client.Graphics;

namespace SS14.Client.UserInterface.Controls
{
    public class OptionButton : Button
    {
        public event Action<ItemSelectedEventArgs> OnItemSelected;

        public int Selected
        {
            get => SceneControl.Selected;
            set => SceneControl.Selected = value;
        }

        public void AddItem(TextureSource icon, string label, int id = 1)
        {
            SceneControl.AddIconItem(icon.Texture, label, id);
        }

        public void AddItem(string label, int id = 1)
        {
            SceneControl.AddItem(label, id);
        }

        public void AddSeparator()
        {
            SceneControl.AddSeparator();
        }

        public void Clear()
        {
            SceneControl.Clear();
        }

        public int ItemCount => SceneControl.GetItemCount();

        public int GetItemId(int idx)
        {
            return SceneControl.GetItemId(idx);
        }

        public object GetItemMetadata(int idx)
        {
            return SceneControl.GetItemMetadata(idx);
        }

        public int SelectedId => SceneControl.GetSelectedId();

        public object SelectedMetadata => SceneControl.GetSceneInstanceLoadPlaceholder();

        public bool IsItemDisabled(int idx)
        {
            return SceneControl.IsItemDisabled(idx);
        }

        public void RemoveItem(int idx)
        {
            SceneControl.RemoveItem(idx);
        }

        public void Select(int idx)
        {
            SceneControl.Select(idx);
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
            SceneControl.SetItemDisabled(idx, disabled);
        }

        public void SetItemIcon(int idx, TextureSource texture)
        {
            SceneControl.SetItemIcon(idx, texture);
        }

        public void SetItemId(int idx, int id)
        {
            SceneControl.SetItemId(idx, id);
        }

        public void SetItemMetadata(int idx, object metadata)
        {
            SceneControl.SetItemMetadata(idx, metadata);
        }

        public void SetItemText(int idx, string text)
        {
            SceneControl.SetItemText(idx, text);
        }

        public OptionButton() : base()
        {
        }
        public OptionButton(string name) : base(name)
        {
        }
        public OptionButton(Godot.OptionButton button) : base(button)
        {
        }

        new private Godot.OptionButton SceneControl;

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.OptionButton();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.OptionButton)control;
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
            base.SetupSignalHooks();

            __itemSelectedSubscriber.Disconnect(SceneControl, "item_selected");
            __itemSelectedSubscriber.Dispose();
            __itemSelectedSubscriber = null;
        }

        private void __itemSelectedHook(object id)
        {
            OnItemSelected?.Invoke(new ItemSelectedEventArgs((int)id, this));
        }
    }
}
