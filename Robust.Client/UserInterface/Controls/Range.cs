using System;
using System.Diagnostics.Contracts;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    public abstract class Range : Control
    {
        private float _maxValue = 100;
        private float _minValue;
        private float _value;
        private float _page;

        public event Action<Range> OnValueChanged;

        public float GetAsRatio()
        {
            return (_value - _minValue) / (_maxValue - _minValue);
        }

        [ViewVariables]
        public float Page
        {
            get => _page;
            set
            {
                _page = value;
                _ensureValueClamped();
            }
        }

        [ViewVariables]
        public float MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = value;
                _ensureValueClamped();
            }
        }

        [ViewVariables]
        public float MinValue
        {
            get => _minValue;
            set
            {
                _minValue = value;
                _ensureValueClamped();
            }
        }

        [ViewVariables]
        public virtual float Value
        {
            get => _value;
            set
            {
                var newValue = ClampValue(value);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (newValue != _value)
                {
                    _value = newValue;
                    OnValueChanged?.Invoke(this);
                }
            }
        }

        private void _ensureValueClamped()
        {
            var newValue = ClampValue(_value);
            if (!FloatMath.CloseTo(newValue, _value))
            {
                _value = newValue;
                OnValueChanged?.Invoke(this);
            }
        }

        [Pure]
        protected float ClampValue(float value)
        {
            return value.Clamp(_minValue, _maxValue-_page);
        }
    }
}
