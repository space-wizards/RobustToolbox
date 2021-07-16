using System.Linq;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables.Editors
{
    public class VVPropEditorKeyValuePair : VVPropEditor
    {
        [Dependency] private readonly IViewVariablesManagerInternal _viewVariables = default!;

        private VVPropEditor? _propertyEditorK;
        private VVPropEditor? _propertyEditorV;

        public VVPropEditorKeyValuePair()
        {
            IoCManager.InjectDependencies(this);
        }

        protected override Control MakeUI(object? value)
        {
            var hBox = new BoxContainer
            {
            	Orientation = LayoutOrientation.Horizontal
            };

            dynamic d = value!;

            // NOTE: value can be both a KeyValuePair<,> here OR as a ServerKeyValuePairToken.

            object? valueK = d.Key;
            object? valueV = d.Value;

            // ReSharper disable ConstantConditionalAccessQualifier
            var typeK = valueK?.GetType();
            var typeV = valueV?.GetType();
            // ReSharper restore ConstantConditionalAccessQualifier

            _propertyEditorK = _viewVariables.PropertyFor(typeK);
            _propertyEditorV = _viewVariables.PropertyFor(typeV);

            var controlK = _propertyEditorK.Initialize(valueK, true);
            var controlV = _propertyEditorV.Initialize(valueV, true);

            hBox.AddChild(controlK);
            hBox.AddChild(controlV);

            return hBox;
        }

        public override void WireNetworkSelector(uint sessionId, object[] selectorChain)
        {
            var keySelector = new ViewVariablesSelectorKeyValuePair {Key = true};
            var valueSelector = new ViewVariablesSelectorKeyValuePair {Key = false};

            var keyChain = selectorChain.Append(keySelector).ToArray();
            var valueChain = selectorChain.Append(valueSelector).ToArray();

            _propertyEditorK!.WireNetworkSelector(sessionId, keyChain);
            _propertyEditorV!.WireNetworkSelector(sessionId, valueChain);
        }
    }
}
