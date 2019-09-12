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
            ClearButtons();
            AddLeftButton(-1, "-");
            AddRightButton(1, "+");
            UpdateButtonOrder();
        }

        public void AddRightButton(int num, string text)
        {
            var button = new Button { Text = text };
            button.OnPressed += (args) => Value += num;
            _rightButtons.Add(button);
        }

        public void AddLeftButton(int num, string text)
        {
            var button = new Button { Text = text };
            button.OnPressed += (args) => Value += num;
            _leftButtons.Add(button);
        }

        public void SetButtons(List<int> leftButtons, List<int> rightButtons)
        {
            ClearButtons();
            foreach (var num in leftButtons)
            {
                AddLeftButton(num, num.ToString());
            }
            foreach (var num in rightButtons)
            {
                AddRightButton(num, num.ToString());
            }
            UpdateButtonOrder();
        }

        public void ClearButtons()
        {
            foreach (var button in _leftButtons)
            {
                button.Dispose();
            }
            _leftButtons.Clear();
            foreach (var button in _rightButtons)
            {
                button.Dispose();
            }
            _rightButtons.Clear();
        }

        public void UpdateButtonOrder()
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
