using System.Globalization;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;

namespace SS14.Client.ViewVariables.Editors
{
    public class ViewVariablesPropertyEditorBox2 : ViewVariablesPropertyEditor
    {
        private readonly bool _intBox;

        public ViewVariablesPropertyEditorBox2(bool intBox)
        {
            _intBox = intBox;
        }

        protected override Control MakeUI(object value)
        {
            var hBoxContainer = new HBoxContainer
            {
                CustomMinimumSize = new Vector2(200, 0),
            };

            var left = new LineEdit
            {
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                PlaceHolder = "Left",
                ToolTip = "Left"
            };

            hBoxContainer.AddChild(left);

            var top = new LineEdit
            {
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                PlaceHolder = "Top",
                ToolTip = "Top"
            };

            hBoxContainer.AddChild(top);


            var right = new LineEdit
            {
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                PlaceHolder = "Right",
                ToolTip = "Right"
            };

            hBoxContainer.AddChild(right);


            var bottom = new LineEdit
            {
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                PlaceHolder = "Bottom",
                ToolTip = "Bottom"
            };

            hBoxContainer.AddChild(bottom);

            if (_intBox)
            {
                var box = (Box2i) value;
                left.Text = box.Left.ToString(CultureInfo.InvariantCulture);
                top.Text = box.Top.ToString(CultureInfo.InvariantCulture);
                right.Text = box.Right.ToString(CultureInfo.InvariantCulture);
                bottom.Text = box.Bottom.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                var box = (Box2) value;
                left.Text = box.Left.ToString(CultureInfo.InvariantCulture);
                top.Text = box.Top.ToString(CultureInfo.InvariantCulture);
                right.Text = box.Right.ToString(CultureInfo.InvariantCulture);
                bottom.Text = box.Bottom.ToString(CultureInfo.InvariantCulture);
            }

            void OnEntered(LineEdit.LineEditEventArgs e)
            {
                if (_intBox)
                {
                    var leftVal = int.Parse(left.Text, CultureInfo.InvariantCulture);
                    var topVal = int.Parse(top.Text, CultureInfo.InvariantCulture);
                    var rightVal = int.Parse(right.Text, CultureInfo.InvariantCulture);
                    var bottomVal = int.Parse(bottom.Text, CultureInfo.InvariantCulture);

                    ValueChanged(new Box2i(leftVal, topVal, rightVal, bottomVal));
                }
                else
                {
                    var leftVal = float.Parse(left.Text, CultureInfo.InvariantCulture);
                    var topVal = float.Parse(top.Text, CultureInfo.InvariantCulture);
                    var rightVal = float.Parse(right.Text, CultureInfo.InvariantCulture);
                    var bottomVal = float.Parse(bottom.Text, CultureInfo.InvariantCulture);

                    ValueChanged(new Box2(leftVal, topVal, rightVal, bottomVal));
                }
            }

            if (!ReadOnly)
            {
                left.OnTextEntered += OnEntered;
                top.OnTextEntered += OnEntered;
                right.OnTextEntered += OnEntered;
                bottom.OnTextEntered += OnEntered;
            }

            return hBoxContainer;
        }
    }
}
