using System;
using System.Diagnostics.Contracts;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public abstract class Range : Control
    {
        private float _maxValue = 100;
        private float _minValue;
        private float _value;
        private float _page;
        private bool _rounded;

        public event Action<Range>? OnValueChanged;

        public float GetAsRatio()
        {
            return (_value - _minValue) / (_maxValue - _minValue);
        }

        public void SetAsRatio(float value)
        {
            Value = ClampValue(value * (_maxValue - _minValue) + _minValue);
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

        [ViewVariables]
        public virtual bool Rounded
        {
            get => _rounded;
            set
            {
                _rounded = value;
                _ensureValueClamped();
            }
        }

        public virtual void SetValueWithoutEvent(float newValue)
        {
            newValue = ClampValue(newValue);
            _value = newValue;
        }

        private void _ensureValueClamped()
        {
            var newValue = ClampValue(_value);
            if (!MathHelper.CloseToPercent(newValue, _value))
            {
                _value = newValue;
                OnValueChanged?.Invoke(this);
            }
        }

        [Pure]
        protected float ClampValue(float value)
        {
            if (_rounded)
            {
                value = MathF.Round(value);
            }
            return MathHelper.Clamp(value, _minValue, _maxValue-_page);
        }
    }
}
