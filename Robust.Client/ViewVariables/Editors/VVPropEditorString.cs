using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Robust.Client.ViewVariables.Editors
{
    internal sealed class VVPropEditorString : VVPropEditor
    {
        protected override Control MakeUI(object? value)
        {
            var lineEdit = new LineEdit
            {
                Text = (string) value!,
                Editable = !ReadOnly,
                HorizontalExpand = true,
            };

            if (!ReadOnly)
            {
                lineEdit.OnTextEntered += e => ValueChanged(e.Text);
            }

            return lineEdit;
        }
    }
}
