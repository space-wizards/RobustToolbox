using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Robust.Client.ViewVariables.Editors
{
    public class ViewVariablesPropertyEditorKeyValuePair : ViewVariablesPropertyEditor
    {
#pragma warning disable 649
        [Dependency] private readonly IViewVariablesManagerInternal _viewVariables;
#pragma warning restore 649

        public ViewVariablesPropertyEditorKeyValuePair()
        {
            IoCManager.InjectDependencies(this);
        }

        protected override Control MakeUI(object value)
        {
            var hBox = new HBoxContainer();

            var propK = value.GetType().GetProperty("Key");
            var propV = value.GetType().GetProperty("Value");

            var valueK = propK.GetValue(value);
            var valueV = propV.GetValue(value);

            var typeK = valueK?.GetType();
            var typeV = valueV?.GetType();

            var propertyEditorK = _viewVariables.PropertyFor(typeK);
            var propertyEditorV = _viewVariables.PropertyFor(typeV);

            WireReference(propertyEditorK, valueK);
            WireReference(propertyEditorV, valueV);

            var controlK = propertyEditorK.Initialize(valueK, true);
            var controlV = propertyEditorV.Initialize(valueV, true);

            hBox.AddChild(controlK);
            hBox.AddChild(controlV);

            return hBox;
        }

        private void WireReference(ViewVariablesPropertyEditor prop, object value)
        {
            if (!(prop is ViewVariablesPropertyEditorReference reference))
            {
                return;
            }

            // TODO: Won't work when networked, fix this.
            reference.OnPressed += () => _viewVariables.OpenVV(value);
        }
    }
}
