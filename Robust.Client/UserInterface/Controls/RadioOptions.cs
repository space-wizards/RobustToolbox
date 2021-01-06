using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Robust.Client.UserInterface.Controls
{
    public class RadioOptions : Control
    {
        public enum RadioLayout { Horizontal, Vertical }

        private readonly List<ButtonData> _buttonDataList = new();
        private readonly Dictionary<int, int> _idMap = new();
        private ButtonGroup _buttonGroup = new();
        private Container _container;

        public string ButtonStyle = "";
        public string FirstButtonStyle = "";
        public string LastButtonStyle = "";

        public int ItemCount => _buttonDataList.Count;

        public event Action<ItemSelectedEventArgs>? OnItemSelected;

        public RadioOptions(RadioLayout layout)
        {
            switch (layout)
            {
                case RadioLayout.Vertical:
                    _container = new VBoxContainer();
                    break;
                case RadioLayout.Horizontal:
                default:
                    _container = new HBoxContainer();
                    break;
            }

            this.AddChild(_container);
        }

        public void AddItem(Texture icon, string label, int? id = null)
        {
            AddItem(label, id);
        }

        public void AddItem(string label, int? id = null, Action<ItemSelectedEventArgs>? itemSelectedAction = null)
        {
            if (id == null)
            {
                id = _buttonDataList.Count;
            }

            if (_idMap.ContainsKey(id.Value))
            {
                throw new ArgumentException("An item with the same ID already exists.");
            }

            var button = new Button
            {
                Text = label,
                ToggleMode = true,
                Group = _buttonGroup
            };
            button.OnPressed += ButtonOnPressed;
            var data = new ButtonData(label, button)
            {
                Id = id.Value,
            };
            if (itemSelectedAction != null)
            {
                data.OnItemSelected += itemSelectedAction;
            }
            _idMap.Add(id.Value, _buttonDataList.Count);
            _buttonDataList.Add(data);
            _container.AddChild(button);
            UpdateFirstAndLastButtonStyle();
            if (_buttonDataList.Count == 1)
            {
                Select(0);
            }
        }

        private void ButtonOnPressed(ButtonEventArgs obj)
        {
            foreach (var buttonData in _buttonDataList)
            {
                if (buttonData.Button == obj.Button)
                {
                    if (buttonData.HasOnItemSelectedEvent)
                        buttonData.InvokeItemSelected(new ItemSelectedEventArgs(buttonData.Id, this));
                    else
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
            foreach (var buttonDatum in _buttonDataList)
            {
                buttonDatum.Button.OnPressed -= ButtonOnPressed;
            }
            _buttonDataList.Clear();
            SelectedId = 0;
        }

        public int GetItemId(int idx)
        {
            return _buttonDataList[idx].Id;
        }

        public object? GetItemMetadata(int idx)
        {
            return _buttonDataList[idx].Metadata;
        }

        public int SelectedId { get; private set; }

        public object? SelectedMetadata => _buttonDataList[_idMap[SelectedId]].Metadata;

        public bool IsItemDisabled(int idx)
        {
            return _buttonDataList[idx].Disabled;
        }

        public void RemoveItem(int idx)
        {
            var data = _buttonDataList[idx];
            data.Button.OnPressed -= ButtonOnPressed;
            _idMap.Remove(data.Id);
            _container.RemoveChild(data.Button);
            _buttonDataList.RemoveAt(idx);
            var newIdx = 0;
            foreach (var buttonData in _buttonDataList)
            {
                _idMap[buttonData.Id] = newIdx++;
            }
            UpdateFirstAndLastButtonStyle();
        }

        public void Select(int idx)
        {
            var data = _buttonDataList[idx];
            SelectedId = data.Id;
            data.Button.Pressed = true;
        }

        public void UpdateFirstAndLastButtonStyle()
        {
            for (int i = 0; i < _buttonDataList.Count; i++)
            {
                var buttonData = _buttonDataList[i];
                if (buttonData.Button == null) continue;

                buttonData.Button.StyleClasses.Remove(ButtonStyle);
                buttonData.Button.StyleClasses.Remove(LastButtonStyle);
                buttonData.Button.StyleClasses.Remove(FirstButtonStyle);

                if (i == 0)
                    buttonData.Button.StyleClasses.Add(FirstButtonStyle);
                else if (i == _buttonDataList.Count - 1)
                    buttonData.Button.StyleClasses.Add(LastButtonStyle);
                else
                    buttonData.Button.StyleClasses.Add(ButtonStyle);
            }
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
            var data = _buttonDataList[idx];
            data.Disabled = disabled;
            data.Button.Disabled = disabled;
        }

        public void SetItemId(int idx, int id)
        {
            if (_idMap.TryGetValue(id, out var existIdx) && existIdx != idx)
            {
                throw new InvalidOperationException("An item with said ID already exists.");
            }

            var data = _buttonDataList[idx];
            _idMap.Remove(data.Id);
            _idMap.Add(id, idx);
            data.Id = id;
        }

        public void SetItemMetadata(int idx, object metadata)
        {
            _buttonDataList[idx].Metadata = metadata;
        }

        public void SetItemText(int idx, string text)
        {
            var data = _buttonDataList[idx];
            data.Text = text;
            data.Button.Text = text;
        }

        public class ItemSelectedEventArgs : EventArgs
        {
            public RadioOptions Button { get; }

            /// <summary>
            ///     The ID of the item that has been selected.
            /// </summary>
            public int Id { get; }

            public ItemSelectedEventArgs(int id, RadioOptions button)
            {
                Id = id;
                Button = button;
            }
        }

        private sealed class ButtonData
        {
            public string Text;
            public bool Disabled;
            public object? Metadata;
            public int Id;
            public Button Button;

            public ButtonData(string text, Button button)
            {
                Text = text;
                Button = button;
            }

            public event Action<ItemSelectedEventArgs>? OnItemSelected;
            public bool HasOnItemSelectedEvent => OnItemSelected != null;
            public void InvokeItemSelected(ItemSelectedEventArgs args)
            {
                OnItemSelected?.Invoke(args);
            }
        }
    }
}
