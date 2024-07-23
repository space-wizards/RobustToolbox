using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Robust.Client.ViewVariables.Editors;

internal sealed class VVPropEditorEntProtoId : VVPropEditor
{
    protected override Control MakeUI(object? value)
    {
        var lineEdit = new LineEdit
        {
            Text = (EntProtoId) (value ?? ""),
            Editable = !ReadOnly,
            HorizontalExpand = true,
        };

        if (!ReadOnly)
        {
            lineEdit.OnTextEntered += e =>
            {
                ValueChanged((EntProtoId) e.Text);
            };
        }

        return lineEdit;
    }
}
