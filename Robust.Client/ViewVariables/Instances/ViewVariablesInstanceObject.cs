using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.ViewVariables.Traits;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.ViewVariables.Editors;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables.Instances
{
    internal class ViewVariablesInstanceObject : ViewVariablesInstance
    {
        private TabContainer _tabs;
        private int _tabCount = 0;

        private readonly List<ViewVariablesTrait> _traits = new List<ViewVariablesTrait>();

        public ViewVariablesRemoteSession Session { get; private set; }
        public object Object { get; private set; }

        public ViewVariablesInstanceObject(IViewVariablesManagerInternal vvm) : base(vvm)
        {
        }

        public override void Initialize(SS14Window window, object obj)
        {
            Object = obj;
            var type = obj.GetType();

            _wrappingInit(window, obj.ToString(), type.ToString());
            foreach (var trait in TraitsFor(ViewVariablesManager.TraitIdsFor(type)))
            {
                trait.Initialize(this);
                _traits.Add(trait);
            }
            _refresh();
        }

        public override void Initialize(SS14Window window,
            ViewVariablesBlobMetadata blob, ViewVariablesRemoteSession session)
        {
            Session = session;

            _wrappingInit(window, $"[SERVER] {blob.Stringified}", blob.ObjectTypePretty);
            foreach (var trait in TraitsFor(blob.Traits))
            {
                trait.Initialize(this);
                _traits.Add(trait);
            }
            _refresh();
        }

        private void _wrappingInit(SS14Window window, string top, string bottom)
        {
            // Wrapping containers.
            var scrollContainer = new ScrollContainer();
            scrollContainer.SetAnchorPreset(Control.LayoutPreset.Wide, true);
            window.Contents.AddChild(scrollContainer);
            var vBoxContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                SizeFlagsVertical = Control.SizeFlags.FillExpand,
            };
            scrollContainer.AddChild(vBoxContainer);

            // Handle top bar.
            {
                var headBox = new HBoxContainer();
                var name = MakeTopBar(top, bottom);
                name.SizeFlagsHorizontal = Control.SizeFlags.FillExpand;
                headBox.AddChild(name);

                var button = new Button {Text = "Refresh"};
                button.OnPressed += _ => _refresh();
                headBox.AddChild(button);
                vBoxContainer.AddChild(headBox);
            }

            _tabs = new TabContainer();
            vBoxContainer.AddChild(_tabs);
        }

        public override void Close()
        {
            base.Close();

            if (Session != null && !Session.Closed)
            {
                ViewVariablesManager.CloseSession(Session);
            }
        }

        public void AddTab(string title, Control control)
        {
            _tabs.AddChild(control);
            _tabs.SetTabTitle(_tabCount++, title);
        }

        private void _refresh()
        {
            // TODO: I'm fully aware the ToString() isn't updated.
            // Eh.
            foreach (var trait in _traits)
            {
                trait.Refresh();
            }
        }

        private static List<ViewVariablesTrait> TraitsFor(ICollection<object> traitData)
        {
            var list = new List<ViewVariablesTrait>(traitData.Count);
            if (traitData.Contains(ViewVariablesTraits.Members))
            {
                list.Add(new ViewVariablesTraitMembers());
            }

            if (traitData.Contains(ViewVariablesTraits.Enumerable))
            {
                list.Add(new ViewVariablesTraitEnumerable());
            }

            return list;
        }
    }
}
