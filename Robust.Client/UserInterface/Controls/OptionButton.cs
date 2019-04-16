using System;
using System.Collections.Generic;
using Robust.Client.GodotGlue;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.OptionButton))]
    public class OptionButton : Button
    {
        private List<ButtonData> _buttonData = new List<ButtonData>();
        private Dictionary<int, int> _idMap = new Dictionary<int, int>();
        private Popup _popup;
        private VBoxContainer _popupVBox;
        private int _selectedId;

        public event Action<ItemSelectedEventArgs> OnItemSelected;

        public int Selected
        {
            get => GameController.OnGodot ? (int) SceneControl.Get("selected") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("selected", value);
                }
            }
        }

        public void AddItem(Texture icon, string label, int? id = null)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("add_icon_item", icon.GodotTexture, label, id ?? -1);
            }
            else
            {
                AddItem(label, id);
            }
        }

        public void AddItem(string label, int? id = null)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("add_item", label, id ?? -1);
            }
            else
            {
                if (id == null)
                {
                    id = _buttonData.Count;
                }

                if (_idMap.ContainsKey(id.Value))
                {
                    throw new ArgumentException("An item with the same ID already exists.");
                }

                var button = new Button
                {
                    Text = label,
                    ToggleMode = true
                };
                button.OnPressed += ButtonOnPressed;
                var data = new ButtonData
                {
                    Text = label,
                    Id = id.Value,
                    Button = button
                };
                _idMap.Add(id.Value, _buttonData.Count);
                _buttonData.Add(data);
                _popupVBox.AddChild(button);
                if (_buttonData.Count == 1)
                {
                    Select(0);
                }
            }
        }

        private void ButtonOnPressed(ButtonEventArgs obj)
        {
            obj.Button.Pressed = false;
            _popup.Visible = false;
            foreach (var buttonData in _buttonData)
            {
                if (buttonData.Button == obj.Button)
                {
                    OnItemSelected?.Invoke(new ItemSelectedEventArgs(buttonData.Id, this));
                    return;
                }
            }

            // Not reachable.
            throw new InvalidOperationException();
        }

        public void Clear()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("clear");
            }
            else
            {
                _idMap.Clear();
                _buttonData.Clear();
                _popupVBox.DisposeAllChildren();
                _selectedId = 0;
            }
        }

        public int ItemCount => GameController.OnGodot ? (int) SceneControl.Call("get_item_count") : _buttonData.Count;

        public int GetItemId(int idx)
        {
            return GameController.OnGodot ? (int) SceneControl.Call("get_item_id", idx) : _buttonData[idx].Id;
        }

        public object GetItemMetadata(int idx)
        {
            return GameController.OnGodot ? SceneControl.Call("get_item_metadata", idx) : _buttonData[idx].Metadata;
        }

        public int SelectedId => GameController.OnGodot ? (int) SceneControl.Call("get_selected_id") : _selectedId;

        public object SelectedMetadata =>
            GameController.OnGodot
                ? SceneControl.GetSceneInstanceLoadPlaceholder()
                : _buttonData[_idMap[_selectedId]].Metadata;

        public bool IsItemDisabled(int idx)
        {
            return GameController.OnGodot
                ? (bool) SceneControl.Call("is_item_disabled", idx)
                : _buttonData[idx].Disabled;
        }

        public void RemoveItem(int idx)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("remove_item", idx);
            }
            else
            {
                var data = _buttonData[idx];
                _idMap.Remove(data.Id);
                _popupVBox.RemoveChild(data.Button);
                _buttonData.RemoveAt(idx);
            }
        }

        public void Select(int idx)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("select", idx);
            }
            else
            {
                var prev = _buttonData[_idMap[_selectedId]];
                prev.Button.Pressed = false;
                var data = _buttonData[idx];
                _selectedId = data.Id;
                Text = data.Text;
                data.Button.Pressed = true;
            }
        }

        public void SelectId(int id)
        {
            Select(GetIdx(id));
        }

        public int GetIdx(int id)
        {
            if (!GameController.OnGodot)
            {
                return _idMap[id];
            }

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
            else
            {
                var data = _buttonData[idx];
                data.Disabled = disabled;
                data.Button.Disabled = disabled;
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
            else
            {
                if (_idMap.TryGetValue(id, out var existIdx) && existIdx != id)
                {
                    throw new InvalidOperationException("An item with said ID already exists.");
                }

                var data = _buttonData[idx];
                _idMap.Remove(data.Id);
                _idMap.Add(id, idx);
                data.Id = id;
            }
        }

        public void SetItemMetadata(int idx, object metadata)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_metadata", idx, metadata);
            }
            else
            {
                _buttonData[idx].Metadata = metadata;
            }
        }

        public void SetItemText(int idx, string text)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_item_text", idx, text);
            }
            else
            {
                var data = _buttonData[idx];
                data.Text = text;
                if (_selectedId == data.Id)
                {
                    Text = text;
                }

                data.Button.Text = text;
            }
        }

        private void _onPressed(ButtonEventArgs args)
        {
            var (minX, minY) = _popupVBox.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(0, 0, Math.Max(minX, Width), minY);
            _popup.Open(box);
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

        protected override void Initialize()
        {
            base.Initialize();

            if (!GameController.OnGodot)
            {
                OnPressed += _onPressed;
                _popup = new Popup();
                AddChild(_popup);
                _popupVBox = new VBoxContainer();
                _popup.AddChild(_popupVBox);
                _popupVBox.SetAnchorAndMarginPreset(LayoutPreset.Wide);
            }
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

        private sealed class ButtonData
        {
            public string Text;
            public bool Disabled;
            public object Metadata;
            public int Id;
            public Button Button;
        }
    }
}
