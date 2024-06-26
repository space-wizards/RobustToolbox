using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Robust.Client.ViewVariables.Editors;

internal sealed class VVPropEditorNullableEntProtoId : VVPropEditor
{
    protected override Control MakeUI(object? value)
    {
        var lineEdit = new LineEdit
        {
            Text = value is EntProtoId protoId ?  protoId.Id : "",
            Editable = !ReadOnly,
            HorizontalExpand = true,
        };

        if (!ReadOnly)
        {
            lineEdit.OnTextEntered += e =>
            {
                if (string.IsNullOrWhiteSpace(e.Text))
                {
                    ValueChanged(null);
                }
                else
                {
                    ValueChanged((EntProtoId) e.Text);
                }
            };
        }

        return lineEdit;
    }
}
