using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables.Editors
{
    public sealed class VVPropEditorEntityUid : VVPropEditor
    {
        protected override Control MakeUI(object? value)
        {
            var hBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                MinSize = new Vector2(200, 0)
            };

            var uid = (EntityUid)value!;
            var lineEdit = new LineEdit
            {
                Text = uid.ToString(),
                Editable = !ReadOnly,
                HorizontalExpand = true,
            };
            if (!ReadOnly)
            {
                lineEdit.OnTextEntered += e =>
                    ValueChanged(EntityUid.Parse(e.Text));
            }

            var vvButton = new Button()
            {
                Text = "View",
            };

            vvButton.OnPressed += e =>
            {
                var id = IoCManager.Resolve<IEntityManager>().GetNetEntity(uid);
                IoCManager.Resolve<IConsoleHost>().ExecuteCommand($"vv {id}");
            };

            hBox.AddChild(lineEdit);
            hBox.AddChild(vvButton);
            return hBox;
        }
    }
}
