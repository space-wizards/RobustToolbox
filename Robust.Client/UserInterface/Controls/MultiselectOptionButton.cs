using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    /// Option button which allows toggling multiple elements.
    /// </summary>
    /// <typeparam name="TKey">type to use as the unique key for each option. Functions similarly
    /// to dictionary key, so the type should make sure to respect dictionary key semantics.</typeparam>
    [Virtual]
    public class MultiselectOptionButton<TKey> : ContainerButton where TKey : notnull
    {
        public const string StyleClassOptionButton = "optionButton";
        public const string StyleClassOptionTriangle = "optionTriangle";

        private List<ButtonData> _buttonData = new();
        // map from key to buttondata index
        private Dictionary<TKey, int> _keyMap = new();
        private readonly Popup _popup;
        private readonly BoxContainer _popupVBox;
        private readonly Label _label;

        public event Action<ItemPressedEventArgs>? OnItemSelected;

        /// <summary>
        /// Tracks the order in which items were selected, latest going at the end.
        /// </summary>
        private List<TKey> _selectedKeys = new();

        /// <summary>
        /// Ids of all currently selected items, ordered by most recently selected = last
        /// </summary>
        public IReadOnlyList<TKey> SelectedKeys => _selectedKeys;

        public int ItemCount => _buttonData.Count;

        /// <summary>
        /// Labels of all currently selected items, ordered by most recently selected = last
        /// </summary>
        public IEnumerable<string?> SelectedLabels => _selectedKeys
            .Select(key => _buttonData[_keyMap[key]].Button.Label.Text);

        /// <summary>
        /// Metadata of all currently selected items, ordered by most recently selected = last
        /// </summary>
        public IEnumerable<object?> SelectedMetadata => _selectedKeys
            .Select(key => _buttonData[_keyMap[key]].Metadata);

        public string? Label
        {
            get => _label.Text;
            set => _label.Text = value;
        }

        public MultiselectOptionButton()
        {
            AddStyleClass(StyleClassButton);
            OnPressed += OnPressedInternal;

            var hBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            AddChild(hBox);

            _popup = new Popup();
            _popupVBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            _popup.AddChild(_popupVBox);
            _popup.OnPopupHide += OnPopupHide;

            _label = new Label
            {
                StyleClasses = { StyleClassOptionButton },
                HorizontalExpand = true,
            };
            hBox.AddChild(_label);

            var textureRect = new TextureRect
            {
                StyleClasses = { StyleClassOptionTriangle },
                VerticalAlignment = VAlignment.Center,
            };
            hBox.AddChild(textureRect);
        }

        public void AddItem(Texture icon, string label, TKey key)
        {
            AddItem(label, key);
        }

        public void AddItem(string label, TKey key)
        {
            if (_keyMap.ContainsKey(key))
            {
                throw new ArgumentException("An item with the same key already exists.");
            }

            var button = new Button
            {
                Text = label,
                ToggleMode = true
            };
            button.OnPressed += ButtonOnPressed;
            var data = new ButtonData(label, button, key);
            _keyMap.Add(key, _buttonData.Count);
            _buttonData.Add(data);
            _popupVBox.AddChild(button);
        }

        private void TogglePopup(bool show)
        {
            if (show)
            {
                var globalPos = GlobalPosition;
                _popupVBox.Measure(Vector2.Infinity);
                var (minX, minY) = _popupVBox.DesiredSize;
                var box = UIBox2.FromDimensions(globalPos, (Math.Max(minX, Width), minY));
                UserInterfaceManager.ModalRoot.AddChild(_popup);
                _popup.Open(box);
            }
            else
            {
                _popup.Close();
            }
        }

        private void OnPopupHide()
        {
            UserInterfaceManager.ModalRoot.RemoveChild(_popup);
        }


        private void ButtonOnPressed(ButtonEventArgs obj)
        {
            TogglePopup(false);
            foreach (var buttonData in _buttonData)
            {
                if (buttonData.Button == obj.Button)
                {
                    if (obj.Button.Pressed)
                    {
                        _selectedKeys.Add(buttonData.Key);
                    }
                    else
                    {
                        _selectedKeys.Remove(buttonData.Key);
                    }
                    OnItemSelected?.Invoke(new ItemPressedEventArgs(buttonData.Key, obj.Button.Pressed, this));
                    return;
                }
            }

            // Not reachable.
            throw new InvalidOperationException();
        }

        public void Clear()
        {
            _keyMap.Clear();
            foreach (var buttonDatum in _buttonData)
            {
                buttonDatum.Button.OnPressed -= ButtonOnPressed;
            }
            _buttonData.Clear();
            _popupVBox.DisposeAllChildren();
            _selectedKeys = new List<TKey>();
        }

        public TKey GetItemKey(int idx)
        {
            return _buttonData[idx].Key;
        }

        public object? GetItemMetadata(int idx)
        {
            return _buttonData[idx].Metadata;
        }

        public bool IsItemDisabled(int idx)
        {
            return _buttonData[idx].Disabled;
        }

        public void RemoveItem(int idx)
        {
            var data = _buttonData[idx];
            data.Button.OnPressed -= ButtonOnPressed;
            _keyMap.Remove(data.Key);
            _popupVBox.RemoveChild(data.Button);
            _buttonData.RemoveAt(idx);
            var newIdx = 0;
            foreach (var buttonData in _buttonData)
            {
                _keyMap[buttonData.Key] = newIdx++;
            }
        }

        public void Select(int idx)
        {
            var data = _buttonData[idx];
            if (data.Button.Pressed) return;
            _selectedKeys.Add(data.Key);
            data.Button.Pressed = true;
        }

        public void SelectKey(TKey key)
        {
            Select(GetIdx(key));
        }

        public void DeselectAll()
        {
            foreach (var buttonData in _buttonData)
            {
                Deselect(buttonData);
            }
        }

        public void Deselect(int idx)
        {
            Deselect(_buttonData[idx]);
        }

        public void DeselectKey(TKey key)
        {
            Deselect(GetIdx(key));
        }

        private void Deselect(ButtonData data)
        {
            if (!data.Button.Pressed) return;
            _selectedKeys.Remove(data.Key);
            data.Button.Pressed = false;
        }


        public int GetIdx(TKey key)
        {
            return _keyMap[key];
        }

        public void SetItemDisabled(int idx, bool disabled)
        {
            var data = _buttonData[idx];
            data.Disabled = disabled;
            data.Button.Disabled = disabled;
        }

        public void SetItemKey(int idx, TKey key)
        {
            if (_keyMap.TryGetValue(key, out var existIdx) && existIdx != idx)
            {
                throw new InvalidOperationException("An item with said key already exists.");
            }

            var data = _buttonData[idx];
            _keyMap.Remove(data.Key);
            _keyMap.Add(key, idx);
            data.Key = key;
        }

        public void SetItemMetadata(int idx, object metadata)
        {
            _buttonData[idx].Metadata = metadata;
        }

        public void SetItemText(int idx, string text)
        {
            var data = _buttonData[idx];
            data.Text = text;
            data.Button.Text = text;
        }

        private void OnPressedInternal(ButtonEventArgs args)
        {
            TogglePopup(true);
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            TogglePopup(false);
        }

        public sealed class ItemPressedEventArgs : EventArgs
        {
            public readonly MultiselectOptionButton<TKey> Button;
            /// <summary>
            /// True if item is being selected, false if being unselected
            /// </summary>
            public readonly bool Selected;
            /// <summary>
            /// True if item is being deselected, false if being selected
            /// </summary>
            public bool Deselected => !Selected;

            /// <summary>
            /// The key of the item that has been selected or deselected.
            /// </summary>
            public readonly TKey Key;

            public ItemPressedEventArgs(TKey key, bool selected, MultiselectOptionButton<TKey> button)
            {
                Key = key;
                Selected = selected;
                Button = button;
            }
        }

        private sealed class ButtonData
        {
            public string Text;
            public bool Disabled;
            public object? Metadata;
            public TKey Key;
            public Button Button;

            public ButtonData(string text, Button button, TKey key)
            {
                Text = text;
                Button = button;
                Key = key;
            }
        }
    }
}
