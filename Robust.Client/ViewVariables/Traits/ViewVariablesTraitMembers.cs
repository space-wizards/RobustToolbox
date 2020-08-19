using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.ViewVariables.Editors;
using Robust.Client.ViewVariables.Instances;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables.Traits
{
    internal class ViewVariablesTraitMembers : ViewVariablesTrait
    {
        private readonly IViewVariablesManagerInternal _vvm;
        private readonly IResourceCache _resourceCache;

        private VBoxContainer _memberList = default!;

        public override void Initialize(ViewVariablesInstanceObject instance)
        {
            base.Initialize(instance);
            _memberList = new VBoxContainer {SeparationOverride = 0};
            instance.AddTab("Members", _memberList);
        }

        public ViewVariablesTraitMembers(IViewVariablesManagerInternal vvm, IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;
            _vvm = vvm;
        }

        public override async void Refresh()
        {
            _memberList.DisposeAllChildren();

            if (Instance.Object != null)
            {
                foreach (var control in ViewVariablesInstance.LocalPropertyList(Instance.Object,
                    Instance.ViewVariablesManager, _resourceCache))
                {
                    _memberList.AddChild(control);
                }
            }
            else
            {
                DebugTools.AssertNotNull(Instance.Session);

                var blob = await Instance.ViewVariablesManager.RequestData<ViewVariablesBlobMembers>(
                    Instance.Session!, new ViewVariablesRequestMembers());

                var otherStyle = false;
                foreach (var propertyData in blob.Members)
                {
                    var propertyEdit = new ViewVariablesPropertyControl(_vvm, _resourceCache);
                    propertyEdit.SetStyle(otherStyle = !otherStyle);
                    var editor = propertyEdit.SetProperty(propertyData);
                    
                    var selectorChain = new object[] {new ViewVariablesMemberSelector(propertyData.PropertyIndex)};
                    editor.WireNetworkSelector(Instance.Session!.SessionId, selectorChain);
                    editor.OnValueChanged += o =>
                    {
                        Instance.ViewVariablesManager.ModifyRemote(Instance.Session!,
                            selectorChain, o);
                    };

                    _memberList.AddChild(propertyEdit);
                }
            }
        }
    }
}
