using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;

namespace SS14.Client.ViewVariables.Editors
{
    internal sealed class ViewVariablesPropertyEditorString : ViewVariablesPropertyEditor
    {
        protected override Control MakeUI(object value)
        {
            var lineEdit = new LineEdit
            {
                Text = (string) value,
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
            };

            if (!ReadOnly)
            {
                lineEdit.OnTextEntered += e => ValueChanged(e.Text);
            }

            return lineEdit;
        }
    }
}
