using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.ViewVariables.Editors
{
    internal sealed class VVPropEditorBoolean : VVPropEditor
    {
        protected override Control MakeUI(object? value)
        {
            var box = new CheckBox
            {
                Pressed = (bool)value!,
                Disabled = ReadOnly,
                Text = value!.ToString()!,
                MinSize = new Vector2(70, 0)
            };
            if (!ReadOnly)
            {
                box.OnToggled += args => ValueChanged(args.Pressed);
            }
            else
            {
                box.Modulate = new Color(1f, 1f, 1f, 0.3f);
            }
            return box;
        }
    }
}
