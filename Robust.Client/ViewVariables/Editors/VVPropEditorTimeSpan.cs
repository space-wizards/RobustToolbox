using System;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Robust.Client.ViewVariables.Editors
{
    public sealed class VVPropEditorTimeSpan : VVPropEditor
    {
        protected override Control MakeUI(object? value)
        {
            var ts = (TimeSpan) value!;
            var lineEdit = new LineEdit
            {
                Text = ts.ToString(),
                Editable = !ReadOnly,
                MinSize = (240, 0)
            };

            lineEdit.OnTextEntered += e =>
            {
                if (TimeSpan.TryParse(e.Text, out var span))
                    ValueChanged(span);
            };

            return lineEdit;
        }
    }
}
