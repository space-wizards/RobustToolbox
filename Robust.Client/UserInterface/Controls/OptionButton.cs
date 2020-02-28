using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.Label;

namespace Robust.Client.UserInterface.Controls
{
    public class OptionButton : ContainerButton
    {
        public const string StyleClassOptionButton = "optionButton";
        public const string StyleClassOptionTriangle = "optionTriangle";

        private List<ButtonData> _buttonData = new List<ButtonData>();
        private Dictionary<int, int> _idMap = new Dictionary<int, int>();
        private Popup _popup;
        private VBoxContainer _popupVBox;
        private Label _label;

        public event Action<ItemSelectedEventArgs> OnItemSelected;

        public string Prefix { get; set; }

        public OptionButton()
        {
            Prefix = "";
            OnPressed += _onPressed;

            var hBox = new HBoxContainer
            {
                MouseFilter = MouseFilterMode.Ignore
            };
            AddChild(hBox);

            _popup = new Popup();
            UserInterfaceManager.ModalRoot.AddChild(_popup);
            _popupVBox = new VBoxContainer();
            _popup.AddChild(_popupVBox);

            _label = new Label
            {
                StyleClasses = { StyleClassOptionButton },
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                MouseFilter = MouseFilterMode.Ignore
            };
            hBox.AddChild(_label);

            var textureRect = new TextureRect
            {
                StyleClasses = { StyleClassOptionTriangle },
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterMode.Ignore
            };
            hBox.AddChild(textureRect);
        }

        public void AddItem(Texture icon, string label, int? id = null)
        {
            AddItem(label, id);
        }

        public void AddItem(string label, int? id = null)
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
            _idMap.Clear();
            _buttonData.Clear();
            _popupVBox.DisposeAllChildren();
            SelectedId = 0;
        }

        public int ItemCount => _buttonData.Count;

        public int GetItemId(int idx)
        {
            return _buttonData[idx].Id;
        }

        public object GetItemMetadata(int idx)
        {
            return _buttonData[idx].Metadata;
        }

        public int SelectedId { get; private set; }

        public object SelectedMetadata => _buttonData[_idMap[SelectedId]].Metadata;

        public bool IsItemDisabled(int idx)
        {
            return _buttonData[idx].Disabled;
        }

        public void RemoveItem(int idx)
        {
            var data = _buttonData[idx];
            _idMap.Remove(data.Id);
            _popupVBox.RemoveChild(data.Button);
            _buttonData.RemoveAt(idx);
        }

        public void Select(int idx)
        {
            if (_idMap.TryGetValue(SelectedId, out var prevIdx))
            {
                _buttonData[prevIdx].Button.Pressed = false;
            }
            var data = _buttonData[idx];
            SelectedId = data.Id;
            _label.Text = Prefix + data.Text;
            data.Button.Pressed = true;
        }

        public void SelectId(int id)
        {
            Select(GetIdx(id));
        }

        public int GetIdx(int id)
        {
            return _idMap[id];
        }

        public void SetItemDisabled(int idx, bool disabled)
        {
            var data = _buttonData[idx];
            data.Disabled = disabled;
            data.Button.Disabled = disabled;
        }

        public void SetItemIcon(int idx, Texture texture)
        {
        }

        public void SetItemId(int idx, int id)
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

        public void SetItemMetadata(int idx, object metadata)
        {
            _buttonData[idx].Metadata = metadata;
        }

        public void SetItemText(int idx, string text)
        {
            var data = _buttonData[idx];
            data.Text = text;
            if (SelectedId == data.Id)
            {
                _label.Text = text;
            }

            data.Button.Text = text;
        }

        private void _onPressed(ButtonEventArgs args)
        {
            var globalPos = GlobalPosition;
            var (minX, minY) = _popupVBox.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(globalPos, (Math.Max(minX, Width), minY));
            _popup.Open(box);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _popup?.Dispose();
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
