using System.Globalization;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables.Editors
{
    public sealed class VVPropEditorAngle : VVPropEditor
    {
        protected override Control MakeUI(object? value)
        {
            var hBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                MinSize = new Vector2(200, 0)
            };
            var angle = (Angle) value!;
            var lineEdit = new LineEdit
            {
                Text = angle.Degrees.ToString(CultureInfo.InvariantCulture),
                Editable = !ReadOnly,
                HorizontalExpand = true
            };
            if (!ReadOnly)
            {
                lineEdit.OnTextEntered += e =>
                {
                    if (!double.TryParse(e.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                        return;

                    ValueChanged(Angle.FromDegrees(number));
                };
            }

            hBox.AddChild(lineEdit);
            hBox.AddChild(new Label {Text = "deg"});
            return hBox;
        }
    }
}
