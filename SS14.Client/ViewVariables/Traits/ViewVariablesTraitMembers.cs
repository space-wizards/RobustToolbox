using SS14.Client.UserInterface.Controls;
using SS14.Client.ViewVariables.Editors;
using SS14.Client.ViewVariables.Instances;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace SS14.Client.ViewVariables.Traits
{
    internal class ViewVariablesTraitMembers : ViewVariablesTrait
    {
        private VBoxContainer _memberList;

        public override void Initialize(ViewVariablesInstanceObject instance)
        {
            base.Initialize(instance);
            _memberList = new VBoxContainer {SeparationOverride = 0};
            instance.AddTab("Members", _memberList);
        }

        public override async void Refresh()
        {
            _memberList.DisposeAllChildren();

            if (Instance.Object != null)
            {
                foreach (var control in ViewVariablesInstance.LocalPropertyList(Instance.Object,
                    Instance.ViewVariablesManager))
                {
                    _memberList.AddChild(control);
                }
            }
            else
            {
                DebugTools.Assert(Instance.Session != null);

                var blob = await Instance.ViewVariablesManager.RequestData<ViewVariablesBlobMembers>(
                    Instance.Session, new ViewVariablesRequestMembers());

                var otherStyle = false;
                foreach (var propertyData in blob.Members)
                {
                    var propertyEdit = new ViewVariablesPropertyControl();
                    propertyEdit.SetStyle(otherStyle = !otherStyle);
                    var editor = propertyEdit.SetProperty(propertyData);
                    // TODO: should this maybe not be hardcoded?
                    if (editor is ViewVariablesPropertyEditorReference refEditor)
                    {
                        refEditor.OnPressed += () =>
                            Instance.ViewVariablesManager.OpenVV(
                                new ViewVariablesSessionRelativeSelector(Instance.Session.SessionId,
                                    new object[] {new ViewVariablesMemberSelector(propertyData.PropertyIndex)}));
                    }

                    editor.OnValueChanged += o =>
                    {
                        Instance.ViewVariablesManager.ModifyRemote(Instance.Session,
                            new object[] {new ViewVariablesMemberSelector(propertyData.PropertyIndex)}, o);
                    };

                    _memberList.AddChild(propertyEdit);
                }
            }
        }
    }
}
