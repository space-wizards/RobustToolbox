using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using System;

namespace Robust.Client.ViewVariables.Editors
{
    internal class ViewVariablesPropertyEditorString : ViewVariablesPropertyEditor
    {
        protected override Control MakeUI(object value)
        {
            var lineEdit = new LineEdit
            {
                Text = ToText(value),
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
            };

            if (!ReadOnly)
            {
                lineEdit.OnTextEntered += HandleEvent();
            }

            return lineEdit;
        }

        protected virtual Action<LineEdit.LineEditEventArgs> HandleEvent()
        {
            return e => ValueChanged(e.Text);
        }

        protected virtual string ToText(object value)
        {
            return (string) value;
        }
    }
}
