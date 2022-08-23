using System;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class SliderIntInput : Control
    {
        private Slider _slider;
        private SpinBox _spinBox;

        private int _value;

        public int MinValue
        {
            get => (int) _slider.MinValue;
            set => _slider.MinValue = value;
        }

        public int MaxValue
        {
            get => (int)_slider.MaxValue;
            set => _slider.MaxValue = value;
        }

        public int Value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;
                _value = value;

                _slider.Value = value;
                _spinBox.Value = value;

                OnValueChanged?.Invoke(_value);
            }
        }

        public float DivisionRatio
        {
            get => _slider.SizeFlagsStretchRatio;
            set => _slider.SizeFlagsStretchRatio = value;
        }

        public event Action<int>? OnValueChanged;

        public SliderIntInput()
        {
            var hBox = new BoxContainer()
            {
                HorizontalExpand = true,
                Orientation = BoxContainer.LayoutOrientation.Horizontal
            };

            // create slider
            _slider = new Slider
            {
                HorizontalExpand = true
            };
            _slider.OnValueChanged += OnSliderValueChanged;
            hBox.AddChild(_slider);

            // and conected spin box
            _spinBox = new SpinBox
            {
                Value = Value,
                IsValid = ValidateSpinBox,
                HorizontalExpand = true,
                SizeFlagsStretchRatio = 0.3f,
                Margin = new Shared.Maths.Thickness(8, 0)
            };
            _spinBox.ValueChanged += OnSpinBoxChanged;
            hBox.AddChild(_spinBox);

            AddChild(hBox);
        }

        private void OnSliderValueChanged(Range slider)
        {
            Value = (int) slider.Value;
        }

        private void OnSpinBoxChanged(object? sender, ValueChangedEventArgs e)
        {
            Value = e.Value;
        }

        private bool ValidateSpinBox(int i)
        {
            return i >= MinValue && i <= MaxValue;
        }
    }
}
