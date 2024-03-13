using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Robust.Client.ViewVariables.Editors;

internal sealed class VVPropEditorProtoId<T> : VVPropEditor where T : class, IPrototype
{
    protected override Control MakeUI(object? value)
    {
        var lineEdit = new LineEdit
        {
            Text = (ProtoId<T>) (value ?? ""),
            Editable = !ReadOnly,
            HorizontalExpand = true,
        };

        if (!ReadOnly)
        {
            lineEdit.OnTextEntered += e =>
            {
                ValueChanged((ProtoId<T>) e.Text);
            };
        }

        return lineEdit;
    }
}
