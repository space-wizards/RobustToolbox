using Robust.Shared.Maths;
using System;
using System.Globalization;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///  Number input LineEdit with increment buttons.
    /// </summary>
    public class FloatSpinBox : HBoxContainer
    {
        private readonly float _stepSize;
        private readonly byte _precision;
        private readonly LineEdit _lineEdit;
        private float _value;
        private Button _btnLeft;
        private Button _btnRight;

        /// <summary>
        ///     Determines whether the SpinBox value gets changed by the input text.
        /// </summary>
        public Func<float, bool>? IsValid { get; set; }

        public event Action<FloatSpinBoxEventArgs>? OnValueChanged;


        public float Value
        {
            get => _value;
            set
            {
                if (IsValid != null && !IsValid(value))
                {
                    return;
                }

                _value = value;
                UpdateTextValue();
            }
        }

        public FloatSpinBox(): this(.1f, 1)
        {
        }

        public FloatSpinBox(float stepSize, byte precision)
        {
            MouseFilter = MouseFilterMode.Pass;

            _lineEdit = new LineEdit
            {
                MinSize = new Vector2(40, 0),
                HorizontalExpand = true,
            };
            AddChild(_lineEdit);

            Value = 0;

            _lineEdit.OnFocusExit += TextChanged;
            _lineEdit.OnTextEntered += TextChanged;
            // step size can not be lesser than precision
            _stepSize = Math.Max(stepSize, MathF.Pow(10, -precision));
            _precision = precision;
            _btnLeft = AddButton(-_stepSize, "-", 0, SpinBox.LeftButtonStyle);
            _btnRight = AddButton(_stepSize, "+", ChildCount, SpinBox.RightButtonStyle);
        }

        public override bool HasKeyboardFocus()
        {
            return _lineEdit.HasKeyboardFocus();
        }

        private Button AddButton(float change, string text, int pos, string style)
        {
            var btn = new Button
            {
                Text = text,
                StyleClasses = { style }
            };
            btn.OnPressed += args =>
            {
                Value += change;
                OnValueChanged?.Invoke(new FloatSpinBoxEventArgs(this, _value));
            };
            AddChild(btn);
            btn.SetPositionInParent(pos);
            return btn;
        }

        private void TextChanged(LineEdit.LineEditEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Text))
            {
                Value = 0;
                OnValueChanged?.Invoke(new FloatSpinBoxEventArgs(this, _value));
                return;
            }

            if (float.TryParse(args.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var res))
            {
                if (IsValid == null || IsValid(res))
                {
                    Value = MathF.Round(res, _precision);
                    OnValueChanged?.Invoke(new FloatSpinBoxEventArgs(this, _value));
                    return;
                }
            }

            UpdateTextValue();
        }

        private void UpdateTextValue()
        {
            var cursorPos = _lineEdit.CursorPosition;
            _lineEdit.Text = _value.ToString("F"+_precision, CultureInfo.InvariantCulture);
            _lineEdit.CursorPosition = cursorPos;
        }

        protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
        {
            base.MouseWheel(args);

            if (!_lineEdit.HasKeyboardFocus())
            {
                return;
            }

            Value += _stepSize * (args.Delta.Y > 0 ? 1 : -1);
        }

        public class FloatSpinBoxEventArgs : EventArgs
        {
            public FloatSpinBox Control { get; }
            public float Value { get; }

            public FloatSpinBoxEventArgs(FloatSpinBox control, float value)
            {
                Control = control;
                Value = value;
            }
        }

    }
}
