using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;

namespace SS14.Client.ViewVariables.Editors
{
    internal sealed class ViewVariablesPropertyEditorBoolean : ViewVariablesPropertyEditor
    {
        protected override Control MakeUI(object value)
        {
            var box = new CheckBox
            {
                Pressed = (bool)value,
                Disabled = ReadOnly,
                Text = value.ToString(),
                CustomMinimumSize = new Vector2(70, 0)
            };
            if (!ReadOnly)
            {
                box.OnToggled += args => ValueChanged(args.Pressed);
            }
            return box;
        }
    }
}
