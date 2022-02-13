using System.Collections.Generic;
using System.Threading.Tasks;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables.Editors
{
    public sealed class VVPropEditorIPrototype<T> : VVPropEditor
    {
        private object? _localValue;
        private ViewVariablesObjectSelector? _selector;

        private ViewVariablesAddWindow? _addWindow;
        private LineEdit _lineEdit = new();

        protected override Control MakeUI(object? value)
        {
            _localValue = value;

            var hbox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            _lineEdit = new LineEdit()
            {
                HorizontalExpand = true,
                HorizontalAlignment = Control.HAlignment.Stretch,
                PlaceHolder = "Prototype ID",
                Text = value switch
                {
                    IPrototype prototype => prototype.ID,
                    ViewVariablesBlobMembers.PrototypeReferenceToken token => token.ID,
                    _ => string.Empty
                },
                Editable = !ReadOnly
            };

            _lineEdit.OnTextEntered += ev =>
            {
                SetNewValue(ev.Text);
            };

            var list = new Button() { Text = "List", Disabled = ReadOnly };
            var inspect = new Button() { Text = "Inspect" };

            list.OnPressed += OnListButtonPressed;
            inspect.OnPressed += OnInspectButtonPressed;

            hbox.AddChild(_lineEdit);
            hbox.AddChild(list);
            hbox.AddChild(inspect);

            return hbox;
        }

        private async void OnListButtonPressed(BaseButton.ButtonEventArgs obj)
        {
            _addWindow?.Dispose();

            if (_selector == null)
            {
                ClientSideWindowList();
            }
            else
            {
                await ServerSideWindowList();
            }
        }

        private void ClientSideWindowList()
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();

            if (!protoMan.TryGetVariantFrom(typeof(T), out var variant)) return;

            var list = new List<string>();

            foreach (var prototype in protoMan.EnumeratePrototypes(variant))
            {
                list.Add(prototype.ID);
            }

            _addWindow = new ViewVariablesAddWindow(list, "Set Prototype [C]");
            _addWindow.AddButtonPressed += OnAddButtonPressed;
            _addWindow.OpenCentered();
        }

        private async Task ServerSideWindowList()
        {
            if (_selector is not ViewVariablesSessionRelativeSelector selector
                || _localValue is not ViewVariablesBlobMembers.PrototypeReferenceToken protoToken) return;

            var vvm = IoCManager.Resolve<IViewVariablesManagerInternal>();

            if (!vvm.TryGetSession(selector.SessionId, out var session)) return;

            var prototypeBlob = await vvm.RequestData<ViewVariablesBlobAllPrototypes>(session, new ViewVariablesRequestAllPrototypes(protoToken.Variant));

            _addWindow = new ViewVariablesAddWindow(prototypeBlob.Prototypes, "Set Prototype [S]");
            _addWindow.AddButtonPressed += OnAddButtonPressed;
            _addWindow.OpenCentered();
        }

        private void OnAddButtonPressed(ViewVariablesAddWindow.AddButtonPressedEventArgs obj)
        {
            _lineEdit.Text = obj.Entry;
            _addWindow?.Dispose();
            SetNewValue(obj.Entry);
        }

        private void OnInspectButtonPressed(BaseButton.ButtonEventArgs obj)
        {
            var vvm = IoCManager.Resolve<IViewVariablesManager>();

            if(_selector != null)
                vvm.OpenVV(_selector);
            else if (_localValue != null)
                vvm.OpenVV(_localValue);
        }

        private void SetNewValue(string text)
        {
            // Remote variable, therefore we send a new PrototypeReferenceToken.
            if (_selector != null)
            {
                if(_localValue is ViewVariablesBlobMembers.PrototypeReferenceToken token)
                    ValueChanged(new ViewVariablesBlobMembers.PrototypeReferenceToken()
                    {
                        Stringified = token.Variant, Variant = token.Variant, ID = text,
                    }, true);

                return;
            }

            // Local variable, therefore the type T should be valid.
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            if(protoMan.TryIndex(typeof(T), text, out var prototype))
                ValueChanged(prototype, false);

            return;
        }

        public override void WireNetworkSelector(uint sessionId, object[] selectorChain)
        {
            _selector = new ViewVariablesSessionRelativeSelector(sessionId, selectorChain);
        }
    }
}
