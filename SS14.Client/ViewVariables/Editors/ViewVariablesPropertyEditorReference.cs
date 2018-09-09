using System;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace SS14.Client.ViewVariables.Editors
{
    internal sealed class ViewVariablesPropertyEditorReference : ViewVariablesPropertyEditor
    {
        public event Action OnPressed;

        protected override Control MakeUI(object value)
        {
            if (value == null)
            {
                return new Label {Text = "null", Align = Label.AlignMode.Right};
            }

            // NOTE: value is NOT always the actual object.
            // Only thing we can really rely on is that ToString works out correctly.
            // This is because of reference tokens, but due to simplicity the object ref is still passed.

            var button = new Button
            {
                Text = $"Reference: {value}"
            };
            button.OnPressed += _ =>
            {
                OnPressed?.Invoke();
            };
            return button;
        }
    }
}
