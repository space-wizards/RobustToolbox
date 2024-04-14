using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Client.ViewVariables.Editors;

internal sealed class VVPropEditorProtoId<T> : VVPropEditor where T : class, IPrototype
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

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
                var id = (ProtoId<T>)e.Text;

                if (!_protoManager.HasIndex(id))
                {
                    return;
                }

                ValueChanged(id);
            };
        }

        return lineEdit;
    }
}
