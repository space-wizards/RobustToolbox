using System;

namespace Robust.Client.UserInterface.Controls
{
    public class SliderIntInput : Control
    {
        private Slider _slider;
        private SpinBox _spinBox;

        private int _minValue = 0;
        private int _maxValue = 100;
        private int _value = 0;
        private float _divisionRatio = 0.3f;

        public int MinValue
        {
            get => _minValue;
            set
            {
                _slider.MinValue = value;
                _minValue = value;
            }
        }

        public int MaxValue
        {
            get => _maxValue;
            set
            {
                _slider.MaxValue = value;
                _maxValue = value;
            }
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
            get => _divisionRatio;
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
                MinValue = MinValue,
                MaxValue = MaxValue,
                Value = MinValue,
                HorizontalExpand = true
            };
            _slider.OnValueChanged += OnSliderValueChanged;
            hBox.AddChild(_slider);

            // and conected spin box
            _spinBox = new SpinBox
            {
                Value = MinValue,
                IsValid = ValidateSpinBox,
                HorizontalExpand = true,
                SizeFlagsStretchRatio = _divisionRatio,
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
