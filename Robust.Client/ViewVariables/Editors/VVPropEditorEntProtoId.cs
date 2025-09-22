using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Client.ViewVariables.Editors;

internal sealed class VVPropEditorEntProtoId : VVPropEditor
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public VVPropEditorEntProtoId()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override Control MakeUI(object? value)
    {
        var lineEdit = new LineEdit
        {
            Text = (EntProtoId)(value ?? ""),
            Editable = !ReadOnly,
            HorizontalExpand = true,
        };

        if (!ReadOnly)
        {
            lineEdit.OnTextEntered += e =>
            {
                var id = (EntProtoId)e.Text;
                if (_protoMan.HasIndex(id))
                    ValueChanged(id);
            };
        }

        return lineEdit;
    }
}
