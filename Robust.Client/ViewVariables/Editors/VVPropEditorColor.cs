using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.ViewVariables.Editors
{
    public class VVPropEditorColor : VVPropEditor
    {
        protected override Control MakeUI(object? value)
        {
            var lineEdit = new LineEdit
            {
                Text = ((Color)value!).ToHex(),
                Editable = !ReadOnly,
                HorizontalExpand = true,
                ToolTip = "Hex color here",
                PlaceHolder = "Hex color here"
            };

            if (!ReadOnly)
            {
                lineEdit.OnTextEntered += e =>
                {
                    var val = Color.FromHex(e.Text);
                    ValueChanged(val);
                };
            }

            return lineEdit;
        }
    }
}
