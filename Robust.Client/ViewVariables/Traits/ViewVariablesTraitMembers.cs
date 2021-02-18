using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.ViewVariables.Instances;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables.Traits
{
    internal class ViewVariablesTraitMembers : ViewVariablesTrait
    {
        private readonly IViewVariablesManagerInternal _vvm;
        private readonly IRobustSerializer _robustSerializer;

        private VBoxContainer _memberList = default!;

        public override void Initialize(ViewVariablesInstanceObject instance)
        {
            base.Initialize(instance);
            _memberList = new VBoxContainer {SeparationOverride = 0};
            instance.AddTab("Members", _memberList);
        }

        public ViewVariablesTraitMembers(IViewVariablesManagerInternal vvm, IRobustSerializer robustSerializer)
        {
            _robustSerializer = robustSerializer;
            _vvm = vvm;
        }

        public override async void Refresh()
        {
            _memberList.DisposeAllChildren();

            if (Instance.Object != null)
            {
                var first = true;
                foreach (var group in ViewVariablesInstance.LocalPropertyList(Instance.Object,
                    Instance.ViewVariablesManager, _robustSerializer))
                {
                    CreateMemberGroupHeader(
                        ref first,
                        TypeAbbreviation.Abbreviate(group.Key),
                        _memberList);

                    foreach (var control in group)
                    {
                        _memberList.AddChild(control);
                    }
                }
            }
            else
            {
                DebugTools.AssertNotNull(Instance.Session);

                var blob = await Instance.ViewVariablesManager.RequestData<ViewVariablesBlobMembers>(
                    Instance.Session!, new ViewVariablesRequestMembers());

                var otherStyle = false;
                var first = true;
                foreach (var (groupName, groupMembers) in blob.MemberGroups)
                {
                    CreateMemberGroupHeader(ref first, groupName, _memberList);

                    foreach (var propertyData in groupMembers)
                    {
                        var propertyEdit = new ViewVariablesPropertyControl(_vvm, _robustSerializer);
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

        internal static void CreateMemberGroupHeader(ref bool first, string groupName, Control container)
        {
            if (!first)
            {
                container.AddChild(new Control {CustomMinimumSize = (0, 16)});
            }

            first = false;
            container.AddChild(new Label {Text = groupName, FontColorOverride = Color.DarkGray});
        }
    }
}
