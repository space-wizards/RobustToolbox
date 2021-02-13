using System;
using System.Linq;
using System.Collections.Generic;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Robust.Client.UserInterface.Controls
{
    public enum RadioOptionsLayout { Horizontal, Vertical }

    public class RadioOptions<T> : Control
    {
        private int internalIdCount = 0;

        private readonly List<RadioOptionButtonData<T>> _buttonDataList = new();
        //private readonly Dictionary<int, int> _idMap = new();
        private ButtonGroup _buttonGroup = new();
        private Container _container;

        public string ButtonStyle = string.Empty;
        public string FirstButtonStyle = string.Empty;
        public string LastButtonStyle = string.Empty;

        public int ItemCount => _buttonDataList.Count;

        /// <summary>
        /// Called whenever you select a button.
        ///
        /// Note: You should add optionButtons.Select(args.Id); if you want to actually select the button.
        /// </summary>
        public event Action<RadioOptionItemSelectedEventArgs<T>>? OnItemSelected;

        public RadioOptions(RadioOptionsLayout layout)
        {
            switch (layout)
            {
                case RadioOptionsLayout.Vertical:
                    _container = new VBoxContainer();
                    break;
                case RadioOptionsLayout.Horizontal:
                default:
                    _container = new HBoxContainer();
                    break;
            }

            AddChild(_container);
        }

        public int AddItem(string label, T value, Action<RadioOptionItemSelectedEventArgs<T>>? itemSelectedAction = null)
        {
            var button = new Button
            {
                Text = label,
                Group = _buttonGroup
            };

            button.OnPressed += ButtonOnPressed;

            var data = new RadioOptionButtonData<T>(label, value, button)
            {
                Id = internalIdCount++
            };

            if (itemSelectedAction != null)
            {
                data.OnItemSelected += itemSelectedAction;
            }

            _buttonDataList.Add(data);
            _container.AddChild(button);
            UpdateFirstAndLastButtonStyle();

            if (_buttonDataList.Count == 1)
            {
                Select(data.Id);
            }
            return data.Id;
        }


        /// <summary>
        /// This is triggered when the button is pressed via the UI
        /// </summary>
        /// <param name="obj"></param>
        private void ButtonOnPressed(ButtonEventArgs obj)
        {
            var buttonData = _buttonDataList.FirstOrDefault(bd => bd.Button == obj.Button);
            if (buttonData != null)
            {
                InvokeItemSelected(new RadioOptionItemSelectedEventArgs<T>(buttonData.Id, this));
                return;
            }
            // Not reachable.
            throw new InvalidOperationException();
        }

        public void Clear()
        {
            foreach (var buttonDatum in _buttonDataList)
            {
                buttonDatum.Button.OnPressed -= ButtonOnPressed;
            }
            _buttonDataList.Clear();
            _container.Children.Clear();
            SelectedId = 0;
        }

        public object? GetItemMetadata(int idx)
        {
            return _buttonDataList.FirstOrDefault(bd => bd.Id == idx)?.Metadata;
        }

        public int SelectedId { get; private set; }
        public RadioOptionButtonData<T> SelectedButtonData => _buttonDataList.First(bd => bd.Id == SelectedId);
        public Button SelectedButton => SelectedButtonData.Button;
        public string SelectedText => SelectedButtonData.Text;
        public T SelectedValue => SelectedButtonData.Value;

        /// <summary>
        /// Always will return true if itemId is not found.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public bool IsItemDisabled(int idx)
        {
            return _buttonDataList.FirstOrDefault(bd => bd.Id == idx)?.Disabled ?? true;
        }

        public void RemoveItem(int idx)
        {

            var data = _buttonDataList.FirstOrDefault(bd => bd.Id == idx);
            if (data!= null)
            {
                data.Button.OnPressed -= ButtonOnPressed;
                _container.RemoveChild(data.Button);

                var buttonData = _buttonDataList.FirstOrDefault(bd => bd.Id == idx);
                if (buttonData != null)
                    _buttonDataList.Remove(buttonData);

                UpdateFirstAndLastButtonStyle();
            }
        }

        public void Select(int idx)
        {
            var data = _buttonDataList.FirstOrDefault(bd => bd.Id == idx);
            if (data != null)
            {
                SelectedId = data.Id;
                data.Button.Pressed = true;
                return;
            }
            // Not found.
        }

        public void SelectByValue(T value)
        {
            var data = _buttonDataList.FirstOrDefault(bd => EqualityComparer<T>.Default.Equals(bd.Value, value));
            if (data != null)
            {
                Select(data.Id);
            }
        }

        public void InvokeItemSelected(RadioOptionItemSelectedEventArgs<T> args)
        {
            var buttonData = _buttonDataList.FirstOrDefault(bd => bd.Id == args.Id);
            if (buttonData == null) return;

            if (buttonData.HasOnItemSelectedEvent)
                buttonData.InvokeItemSelected(args);
            else
                OnItemSelected?.Invoke(args);
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

        public void SetItemDisabled(int idx, bool disabled)
        {
            var data = _buttonDataList.FirstOrDefault(bd => bd.Id == idx);
            if (data != null)
            {
                data.Disabled = disabled;
                data.Button.Disabled = disabled;
            }
        }

        public void SetItemMetadata(int idx, object metadata)
        {
            var buttonData = _buttonDataList.FirstOrDefault(bd => bd.Id == idx);
            if (buttonData != null)
                buttonData.Metadata = metadata;
        }

        public void SetItemText(int idx, string text)
        {
            var data = _buttonDataList.FirstOrDefault(bd => bd.Id == idx);
            if (data != null)
            {
                data.Text = text;
                data.Button.Text = text;
            }
        }
    }
    public class RadioOptionItemSelectedEventArgs<T> : EventArgs
    {
        public RadioOptions<T> Button { get; }

        /// <summary>
        ///     The ID of the item that has been selected.
        /// </summary>
        public int Id { get; }

        public RadioOptionItemSelectedEventArgs(int id, RadioOptions<T> button)
        {
            Id = id;
            Button = button;
        }
    }

    public sealed class RadioOptionButtonData<T>
    {
        public int Id;
        public string Text;
        public T Value;
        public bool Disabled;
        public object? Metadata;

        public Button Button;

        public RadioOptionButtonData(string text, T value, Button button)
        {
            Text = text;
            Button = button;
            Value = value;
        }
        public event Action<RadioOptionItemSelectedEventArgs<T>>? OnItemSelected;
        public bool HasOnItemSelectedEvent => OnItemSelected != null;
        public void InvokeItemSelected(RadioOptionItemSelectedEventArgs<T> args)
        {
            OnItemSelected?.Invoke(args);
        }
    }
}
