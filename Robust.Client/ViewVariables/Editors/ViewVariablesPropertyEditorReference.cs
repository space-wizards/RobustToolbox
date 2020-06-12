using System;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Robust.Client.ViewVariables.Editors
{
    internal sealed class ViewVariablesPropertyEditorReference : ViewVariablesPropertyEditor
    {
        public event Action? OnPressed;

        protected override Control MakeUI(object? value)
        {
            if (value == null)
            {
                return new Label {Text = "null", Align = Label.AlignMode.Right};
            }

            // NOTE: value is NOT always the actual object.
            // Only thing we can really rely on is that ToString works out correctly.
            // This is because of reference tokens, but due to simplicity the object ref is still passed.
            var toString = PrettyPrint.PrintUserFacing(value);
            var button = new Button
            {
                Text = $"Ref: {toString}",
                ClipText = true,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand
            };
            button.OnPressed += _ =>
            {
                OnPressed?.Invoke();
            };
            return button;
        }
    }
}
