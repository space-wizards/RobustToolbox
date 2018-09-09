using System;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;

namespace SS14.Client.ViewVariables.Editors
{
    public class ViewVariablesPropertyEditorColor : ViewVariablesPropertyEditor
    {
        protected override Control MakeUI(object value)
        {
            var lineEdit = new LineEdit
            {
                Text = ((Color)value).ToHex(),
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
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
