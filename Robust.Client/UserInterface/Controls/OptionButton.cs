using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class OptionButton : PushButton
    {
        public const string StyleClassOptionButton = "optionButton";
        public const string StyleClassPopup = "optionButtonPopup";
        public const string StyleClassOptionTriangle = "optionTriangle";
        public const string StyleClassOptionsBackground = "optionButtonBackground";
        public readonly ScrollContainer OptionsScroll;

        private readonly List<ButtonData> _buttonData = new();
        private readonly Dictionary<int, int> _idMap = new();
        private readonly Popup _popup;
        private readonly BoxContainer _popupContentsBox;
        private readonly Label _label;
        private readonly TextureRect _triangle;
        private readonly LineEdit _filterBox;

        public int ItemCount => _buttonData.Count;

        /// <summary>
        /// If true, hides the triangle that normally appears to the right of the button label
        /// </summary>
        public bool HideTriangle
        {
            get => _hideTriangle;
            set
            {
                _hideTriangle = value;
                _triangle.Visible = !_hideTriangle;
            }
        }
        private bool _hideTriangle;
        private bool _filterable;

        /// <summary>
        /// StyleClasses to apply to the options that popup when clicking this button.
        /// </summary>
        public ICollection<string> OptionStyleClasses { get; }

        public event Action<ItemSelectedEventArgs>? OnItemSelected;

        public string Prefix { get; set; } = string.Empty;
        public bool PrefixMargin { get; set; } = true;

        public bool Filterable
        {
            get => _filterable;
            set
            {
                _filterable = value;
                _filterBox.Visible = value;
                UpdateFilters();
            }
        }

        public OptionButton()
        {
            OptionStyleClasses = new List<string>();
            OnPressed += OnPressedInternal;

            var hBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            AddChild(hBox);

            var popupVBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical, Children =
                {
                    (_filterBox = new LineEdit
                    {
                        PlaceHolder = Loc.GetString("option-button-filter"),
                        SelectAllOnFocus = true,
                        Visible = false,
                    }),
                    (_popupContentsBox = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical
                    })
                }
            };

            OptionsScroll = new()
            {
                Children = { popupVBox },
                ReturnMeasure = true,
                MaxHeight = 300
            };

            _popup = new Popup()
            {
                Children = {
                    new PanelContainer {
                        StyleClasses = { StyleClassOptionsBackground }
                    },
                    OptionsScroll
                },
                StyleClasses = { StyleClassPopup }
            };
            _popup.OnPopupHide += OnPopupHide;

            _label = new Label
            {
                StyleClasses = { StyleClassOptionButton },
                HorizontalExpand = true,
            };
            hBox.AddChild(_label);

            _triangle = new TextureRect
            {
                StyleClasses = { StyleClassOptionTriangle },
                VerticalAlignment = VAlignment.Center,
                Visible = !HideTriangle
            };
            hBox.AddChild(_triangle);

            _filterBox.OnTextChanged += _ =>
            {
                UpdateFilters();
            };
        }

        public void AddItem(Texture icon, string label, int? id = null)
        {
            AddItem(label, id);
        }

        public virtual void ButtonOverride(Button button)
        {

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
            foreach (var styleClass in OptionStyleClasses)
            {
                button.AddStyleClass(styleClass);
            }
            button.OnPressed += ButtonOnPressed;
            var data = new ButtonData(label, button)
            {
                Id = id.Value,
            };
            _idMap.Add(id.Value, _buttonData.Count);
            _buttonData.Add(data);
            _popupContentsBox.AddChild(button);
            if (_buttonData.Count == 1)
            {
                Select(0);
            }

            ButtonOverride(button);
            UpdateFilter(data);
        }

        private void TogglePopup(bool show)
        {
            if (show)
            {
                if (Root == null)
                    throw new InvalidOperationException("No UI root! We can't pop up!");

                var globalPos = GlobalPosition;
                globalPos.Y += Size.Y + 1; // Place it below us, with a safety margin.
                globalPos.Y -= Margin.SumVertical;
                OptionsScroll.Measure(Window?.Size ?? Vector2Helpers.Infinity);
                var (minX, minY) = OptionsScroll.DesiredSize;
                var box = UIBox2.FromDimensions(globalPos, new Vector2(Math.Max(minX, Width), minY));
                Root.ModalRoot.AddChild(_popup);
                _popup.Open(box);

                if (_filterable)
                    _filterBox.GrabKeyboardFocus();
            }
            else
            {
                _popup.Close();
            }
        }

        private void OnPopupHide()
        {
            _popup.Orphan();
        }

        private void ButtonOnPressed(ButtonEventArgs obj)
        {
            obj.Button.Pressed = false;
            TogglePopup(false);
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
            foreach (var buttonDatum in _buttonData)
            {
                buttonDatum.Button.OnPressed -= ButtonOnPressed;
            }
            _buttonData.Clear();
            _popupContentsBox.DisposeAllChildren();
            SelectedId = 0;
        }

        public int GetItemId(int idx)
        {
            return _buttonData[idx].Id;
        }

        public object? GetItemMetadata(int idx)
        {
            return _buttonData[idx].Metadata;
        }

        public int SelectedId { get; private set; }

        public object? SelectedMetadata => _buttonData[_idMap[SelectedId]].Metadata;

        public bool IsItemDisabled(int idx)
        {
            return _buttonData[idx].Disabled;
        }

        public void RemoveItem(int idx)
        {
            var data = _buttonData[idx];
            data.Button.OnPressed -= ButtonOnPressed;
            _idMap.Remove(data.Id);
            _popupContentsBox.RemoveChild(data.Button);
            _buttonData.RemoveAt(idx);
            var newIdx = 0;
            foreach (var buttonData in _buttonData)
            {
                _idMap[buttonData.Id] = newIdx++;
            }
        }

        /// <summary>
        /// Select by index rather than id. Throws exception if item with that index
        /// not in this control.
        /// </summary>
        public void Select(int idx)
        {
            if (_idMap.TryGetValue(SelectedId, out var prevIdx))
            {
                _buttonData[prevIdx].Button.Pressed = false;
            }
            var data = _buttonData[idx];
            SelectedId = data.Id;
            _label.Text = PrefixMargin ? Prefix + " " + data.Text : Prefix + data.Text;
            data.Button.Pressed = true;
        }

        /// <summary>
        /// Select by index rather than id.
        /// </summary>
        /// <returns>false if item with that index not in this control</returns>
        public bool TrySelect(int idx)
        {
            if (idx < 0 || idx >= _buttonData.Count) return false;
            Select(idx);
            return true;
        }

        /// throws exception if item with this ID is not in this control
        public void SelectId(int id)
        {
            Select(GetIdx(id));
        }

        /// <returns>false if item with id not in this control</returns>
        public bool TrySelectId(int id)
        {
            return _idMap.TryGetValue(id, out var idx) && TrySelect(idx);
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

        public void SetItemId(int idx, int id)
        {
            if (_idMap.TryGetValue(id, out var existIdx) && existIdx != idx)
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

        private void OnPressedInternal(ButtonEventArgs args)
        {
            TogglePopup(true);
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            TogglePopup(false);
        }

        private void UpdateFilters()
        {
            foreach (var entry in _buttonData)
            {
                UpdateFilter(entry);
            }
        }

        private void UpdateFilter(ButtonData data)
        {
            if (!_filterable)
            {
                data.Button.Visible = true;
                return;
            }

            data.Button.Visible = data.Text.Contains(_filterBox.Text, StringComparison.CurrentCultureIgnoreCase);
        }

        public sealed class ItemSelectedEventArgs : EventArgs
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
            public object? Metadata;
            public int Id;
            public Button Button;

            public ButtonData(string text, Button button)
            {
                Text = text;
                Button = button;
            }
        }
    }
}
