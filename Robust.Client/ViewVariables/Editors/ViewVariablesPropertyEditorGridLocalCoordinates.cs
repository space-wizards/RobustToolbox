using System.Globalization;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.ViewVariables.Editors
{
    public class ViewVariablesPropertyEditorGridLocalCoordinates : ViewVariablesPropertyEditor
    {
        protected override Control MakeUI(object value)
        {
            var coords = (GridCoordinates) value;
            var hBoxContainer = new HBoxContainer
            {
                CustomMinimumSize = new Vector2(240, 0),
            };

            hBoxContainer.AddChild(new Label {Text = "grid: "});

            var gridId = new LineEdit
            {
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                PlaceHolder = "Grid ID",
                ToolTip = "Grid ID",
                Text = coords.GridID.ToString()
            };

            hBoxContainer.AddChild(gridId);

            hBoxContainer.AddChild(new Label {Text = "pos: "});

            var x = new LineEdit
            {
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                PlaceHolder = "X",
                ToolTip = "X",
                Text = coords.X.ToString(CultureInfo.InvariantCulture)
            };

            hBoxContainer.AddChild(x);

            var y = new LineEdit
            {
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                PlaceHolder = "Y",
                ToolTip = "Y",
                Text = coords.Y.ToString(CultureInfo.InvariantCulture)
            };

            hBoxContainer.AddChild(y);

            void OnEntered(LineEdit.LineEditEventArgs e)
            {
                var gridVal = int.Parse(gridId.Text, CultureInfo.InvariantCulture);
                var xVal = float.Parse(x.Text, CultureInfo.InvariantCulture);
                var yVal = float.Parse(y.Text, CultureInfo.InvariantCulture);

                ValueChanged(new GridCoordinates(xVal, yVal, new GridId(gridVal)));
            }

            if (!ReadOnly)
            {
                gridId.OnTextEntered += OnEntered;
                x.OnTextEntered += OnEntered;
                y.OnTextEntered += OnEntered;
            }

            return hBoxContainer;
        }
    }
}
