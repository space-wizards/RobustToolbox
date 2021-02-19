using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables.Editors
{
    internal sealed class VVPropEditorReference : VVPropEditor
    {
        private object? _localValue;
        private ViewVariablesObjectSelector? _selector;

        protected override Control MakeUI(object? value)
        {
            if (value == null)
            {
                return new Label {Text = "null", Align = Label.AlignMode.Right};
            }

            _localValue = value;

            // NOTE: value is NOT always the actual object.
            // Only thing we can really rely on is that ToString works out correctly.
            // This is because of reference tokens, but due to simplicity the object ref is still passed.
            var toString = PrettyPrint.PrintUserFacing(value);
            var button = new Button
            {
                Text = $"Ref: {toString}",
                ClipText = true,
                HorizontalExpand = true,
            };
            button.OnPressed += ButtonOnOnPressed;
            return button;
        }

        private void ButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            var vvm = IoCManager.Resolve<IViewVariablesManager>();
            if (_selector != null)
            {
                vvm.OpenVV(_selector);
            }
            else if (_localValue != null)
            {
                vvm.OpenVV(_localValue);
            }
        }

        public override void WireNetworkSelector(uint sessionId, object[] selectorChain)
        {
            _selector = new ViewVariablesSessionRelativeSelector(sessionId, selectorChain);
        }
    }
}
