using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables.Editors;

public sealed class VVPropEditorNetEntity : VVPropEditor
{
    protected override Control MakeUI(object? value)
    {
        var hBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            MinSize = new Vector2(200, 0)
        };

        var nuid = (NetEntity)value!;
        var lineEdit = new LineEdit
        {
            Text = nuid.ToString(),
            Editable = !ReadOnly,
            HorizontalExpand = true,
        };
        if (!ReadOnly)
        {
            lineEdit.OnTextEntered += e =>
                ValueChanged(NetEntity.Parse(e.Text));
        }

        var vvButton = new Button()
        {
            Text = "View",
        };

        vvButton.OnPressed += e =>
        {
            IoCManager.Resolve<IConsoleHost>().ExecuteCommand($"vv {nuid}");
        };

        hBox.AddChild(lineEdit);
        hBox.AddChild(vvButton);
        return hBox;
    }
}