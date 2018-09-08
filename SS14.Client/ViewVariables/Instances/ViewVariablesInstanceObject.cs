using System.Reflection;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Client.ViewVariables.Editors;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace SS14.Client.ViewVariables.Instances
{
    internal class ViewVariablesInstanceObject : ViewVariablesInstance
    {
        private VBoxContainer _propertyList;

        private ViewVariablesRemoteSession _session;
        private object _object;

        public ViewVariablesInstanceObject(IViewVariablesManagerInternal vvm) : base(vvm)
        {
        }

        public override void Initialize(SS14Window window, object obj)
        {
            _object = obj;
            var type = obj.GetType();

            _wrappingInit(window, obj.ToString(), type.ToString());
            _refresh();
        }

        public override void Initialize(SS14Window window,
            ViewVariablesBlob blob, ViewVariablesRemoteSession session)
        {
            _session = session;

            _wrappingInit(window, $"[SERVER] {blob.Stringified}", blob.ObjectTypePretty);
            _refresh(blob);
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

            _propertyList = new VBoxContainer { SeparationOverride = 0};
            vBoxContainer.AddChild(_propertyList);
        }

        public override void Close()
        {
            base.Close();

            if (_session != null && !_session.Closed)
            {
                ViewVariablesManager.CloseSession(_session);
            }
        }

        private async void _refresh(ViewVariablesBlob preBlob = null)
        {
            // TODO: I'm fully aware the ToString() isn't updated.
            // Eh.
            if (_object != null)
            {
                // Local object mode.
                _propertyList.DisposeAllChildren();
                foreach (var control in LocalPropertyList(_object, ViewVariablesManager))
                {
                    _propertyList.AddChild(control);
                }
            }
            else
            {
                // Remote object mode.
                DebugTools.Assert(_session != null);

                ViewVariablesBlob blob;
                _propertyList.DisposeAllChildren();
                if (preBlob != null)
                {
                    blob = preBlob;
                }
                else
                {
                    blob = await ViewVariablesManager.RequestData(_session);
                }

                var otherStyle = false;
                foreach (var propertyData in blob.Properties)
                {
                    var propertyEdit = new ViewVariablesPropertyControl();
                    propertyEdit.SetStyle(otherStyle = !otherStyle);
                    var editor = propertyEdit.SetProperty(propertyData);
                    // TODO: should this maybe not be hardcoded?
                    if (editor is ViewVariablesPropertyEditorReference refEditor)
                    {
                        refEditor.OnPressed += () =>
                            ViewVariablesManager.OpenVV(
                                new ViewVariablesSessionRelativeSelector(_session.SessionId, propertyData.Name));
                    }

                    editor.OnValueChanged += o => { ViewVariablesManager.ModifyRemote(_session, propertyData.Name, o); };

                    _propertyList.AddChild(propertyEdit);
                }
            }
        }
    }
}
