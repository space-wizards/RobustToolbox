using System;
using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{

    public sealed class ColorSlider : Control
    {

        private readonly Slider _slider;

        private readonly LineEdit _textBox;

        private byte _colorValue;

        private bool _ignoreEvents;

        public event Action OnValueChanged;

        public byte ColorValue
        {
            get => _colorValue;
            set
            {
                _ignoreEvents = true;
                _colorValue = value;
                _slider.Value = value;
                _textBox.Text = value.ToString();
                _ignoreEvents = false;
            }
        }

        public ColorSlider()
        {
            _slider = new Slider
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MaxValue = Byte.MaxValue
            };
            _textBox = new LineEdit
            {
                CustomMinimumSize = (50, 0)
            };

            AddChild(new HBoxContainer
            {
                Children =
                {
                    _slider,
                    _textBox
                }
            });

            _slider.OnValueChanged += _ =>
            {
                if (_ignoreEvents)
                {
                    return;
                }

                _colorValue = (byte) _slider.Value;
                _textBox.Text = _colorValue.ToString();

                OnValueChanged?.Invoke();
            };

            _textBox.OnTextChanged += ev =>
            {
                if (_ignoreEvents)
                {
                    return;
                }

                if (Int32.TryParse(ev.Text, out var result))
                {
                    result = result.Clamp(0, Byte.MaxValue);

                    _ignoreEvents = true;
                    _colorValue = (byte) result;
                    _slider.Value = result;
                    _ignoreEvents = false;

                    OnValueChanged?.Invoke();
                }
            };
        }

        public ColorSlider(string styleClass) : this()
            => _slider.StyleClasses = new[] {styleClass};

    }

}
