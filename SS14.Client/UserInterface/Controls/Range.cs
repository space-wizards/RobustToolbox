using System;
using System.Diagnostics.Contracts;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Range))]
    public abstract class Range : Control
    {
        private float _maxValue;
        private float _minValue;
        private float _value;
        private float _page;

        public Range()
        {
        }

        public Range(string name) : base(name)
        {
        }

        internal Range(Godot.Range control) : base(control)
        {
        }

        public event Action<Range> OnValueChanged;

        public float GetAsRatio()
        {
            if (GameController.OnGodot)
            {
                return (float)SceneControl.Call("get_as_ratio");
            }

            return (_value - _minValue) / (_maxValue - _minValue);
        }

        public float Page
        {
            get => GameController.OnGodot ? (float)SceneControl.Get("page") : _page;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("page", value);
                }
                else
                {
                    _page = value;
                    _ensureValueClamped();
                }
            }
        }

        public float MaxValue
        {
            get => GameController.OnGodot ? (float)SceneControl.Get("max_value") : _maxValue;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("max_value", value);
                }
                else
                {
                    _maxValue = value;
                    _ensureValueClamped();
                }
            }
        }

        public float MinValue
        {
            get => GameController.OnGodot ? (float)SceneControl.Get("min_value") : _minValue;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("min_value", value);
                }
                else
                {
                    _minValue = value;
                    _ensureValueClamped();
                }
            }
        }

        public float Value
        {
            get => GameController.OnGodot ? (float) SceneControl.Get("value") : _value;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("value", value);
                }
                else
                {
                    var newValue = ClampValue(value);
                    if (!FloatMath.CloseTo(newValue, _value))
                    {
                        _value = newValue;
                        OnValueChanged?.Invoke(this);
                    }
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
