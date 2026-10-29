using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.ViewVariables.Instances;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables.Traits
{
    internal sealed class ViewVariablesTraitMembers : ViewVariablesTrait
    {
        private readonly IClientViewVariablesManagerInternal _vvm;
        private readonly IRobustSerializer _robustSerializer;

        private BoxContainer _memberList = default!;

        public override void Initialize(ViewVariablesInstanceObject instance)
        {
            base.Initialize(instance);
            _memberList = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                SeparationOverride = 0
            };
            instance.AddTab("Members", _memberList);
        }

        public ViewVariablesTraitMembers(IClientViewVariablesManagerInternal vvm, IRobustSerializer robustSerializer)
        {
            _robustSerializer = robustSerializer;
            _vvm = vvm;
        }

        public override async void Refresh()
        {
            List<Control> replacementControls = [];

            if (Instance.Object != null)
            {
                var first = true;
                foreach (var group in ViewVariablesInstance.LocalPropertyList(Instance.Object,
                    Instance.ViewVariablesManager, _robustSerializer))
                {
                    CreateMemberGroupHeader(
                        ref first,
                        PrettyPrint.PrintUserFacingTypeShort(group.Key, 2),
                        replacementControls);

                    foreach (var control in group)
                    {
                        replacementControls.Add(control);
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
                var docTargets = new List<(string, Action<string>)>();
                foreach (var (groupName, groupMembers) in blob.MemberGroups)
                {
                    var prettyGroupName = TypeAbbreviation.Abbreviate(groupName);
                    CreateMemberGroupHeader(ref first, prettyGroupName, replacementControls);

                    foreach (var propertyData in groupMembers)
                    {
                        var propertyEdit = new ViewVariablesPropertyControl(_vvm, _robustSerializer);
                        propertyEdit.SetStyle(otherStyle = !otherStyle);
                        var editor = propertyEdit.SetProperty(propertyData, null);
                        docTargets.Add(($"{groupName}.{propertyData.Name}", propertyEdit.SetDocString));

                        var selectorChain = new object[] {new ViewVariablesMemberSelector(propertyData.PropertyIndex)};
                        editor.WireNetworkSelector(Instance.Session!.SessionId, selectorChain);
                        editor.OnValueChanged += (o, r) =>
                        {
                            Instance.ViewVariablesManager.ModifyRemote(Instance.Session!,
                                selectorChain, o, r);
                        };

                        replacementControls.Add(propertyEdit);
                    }
                }

                // Server members may only have docs on the server, fetch them in one go.
                _vvm.ApplyDocStringsAsync(ViewVariablesDocStringKind.FieldOrProperty, docTargets);
            }

            _memberList.DisposeAllChildren();
            foreach (var item in replacementControls)
            {
                _memberList.AddChild(item);
            }
        }

        internal static void CreateMemberGroupHeader(ref bool first, string groupName, ICollection<Control> container)
        {
            if (!first)
            {
                container.Add(new Control {MinSize = new Vector2(0, 16)});
            }

            first = false;
            var label = new Label { Text = groupName, FontColorOverride = Color.White, Margin = new Thickness(2)};
            container.Add(label);
        }
    }
}
