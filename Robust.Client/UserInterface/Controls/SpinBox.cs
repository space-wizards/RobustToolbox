using Robust.Client.Graphics.Drawing;
using Robust.Shared.Maths;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Input for numbers.
    /// </summary>
    public class SpinBox : BoxContainer
    {
        private protected override bool Vertical => false;

        private LineEdit _lineEdit;
        private List<Button> _leftButtons = new List<Button>();
        private List<Button> _rightButtons = new List<Button>();
        private int _stepSize = 1;

        public int Value
        {
            get => int.TryParse(_lineEdit.Text, out int i) ? i : 0;
            set => _lineEdit.Text = value.ToString();
        }

        public SpinBox() : base()
        {
            _lineEdit = new LineEdit
            {
                CustomMinimumSize = new Vector2(40, 0),
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            AddChild(_lineEdit);

            Value = 0;

            _lineEdit.IsValid = (str) => int.TryParse(str, out int i);
        }

        public void InitDefaultButtons()
        {
            _leftButtons.Add(new Button
            {
                Text = "-"
            });
            _leftButtons[0].OnPressed += (args) => { Value -= 1; };
            AddChild(_leftButtons[0]);

            _rightButtons.Add(new Button
            {
                Text = "+"
            });
            _rightButtons[0].OnPressed += (args) => { Value += 1; };
            AddChild(_rightButtons[0]);
            ReflowVBox();
        }

        public void SetButtons(List<int> leftButtons, List<int> rightButtons)
        {
            // TODO Implement clearing _leftButtons and _rightButtons
            foreach (var num in leftButtons)
            {
                var button = new Button { Text = num.ToString() };
                button.OnPressed += (args) => Value += num;
                _leftButtons.Add(button);
            }
            foreach (var num in rightButtons)
            {
                var button = new Button { Text = num.ToString() };
                button.OnPressed += (args) => Value += num;
                _rightButtons.Add(button);
            }
            ReflowVBox();
        }

        public void ReflowVBox()
        {
            RemoveAllChildren();

            foreach (var button in _leftButtons)
            {
                AddChild(button);
            }
            AddChild(_lineEdit);
            foreach (var button in _rightButtons)
            {
                AddChild(button);
            }
        }
/*
        protected override Vector2 CalculateMinimumSize()
        {
            return _lineEdit.CombinedMinimumSize;
            //_hBox.CombinedMinimumSize;
        }*/

        protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
        {
            base.MouseWheel(args);

            if (args.Delta.Y > 0)
                Value += _stepSize;
            else if (args.Delta.Y < 0)
                Value -= _stepSize;
        }
    }
}
