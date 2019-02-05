using System;
using System.Net.Http.Headers;
using System.Reflection;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.ViewVariables.Editors;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace SS14.Client.ViewVariables
{
    internal class ViewVariablesPropertyControl : PanelContainer
    {
        public VBoxContainer VBox { get; private set; }
        public HBoxContainer TopContainer { get; private set; }
        public HBoxContainer BottomContainer { get; private set; }
        public Label NameLabel { get; private set; }

        private Label _bottomLabel;

        [Dependency] private IViewVariablesManagerInternal _viewVariablesManager;

        protected override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            MouseFilter = MouseFilterMode.Stop;
            ToolTip = "Click to expand";
            CustomMinimumSize = new Vector2(0, 25);

            VBox = new VBoxContainer {SeparationOverride = 0};
            AddChild(VBox);

            TopContainer = new HBoxContainer {SizeFlagsVertical = SizeFlags.FillExpand};
            VBox.AddChild(TopContainer);

            BottomContainer = new HBoxContainer
            {
                Visible = false
            };
            VBox.AddChild(BottomContainer);

            var resc = IoCManager.Resolve<IResourceCache>();
            var smallFont = new VectorFont(resc.GetResource<FontResource>("/Fonts/CALIBRI.TTF"), 10);

            _bottomLabel = new Label
            {
                FontOverride = smallFont,
                FontColorOverride = Color.DarkGray
            };
            BottomContainer.AddChild(_bottomLabel);

            NameLabel = new Label();
            TopContainer.AddChild(NameLabel);
        }

        public ViewVariablesPropertyEditor SetProperty(ViewVariablesBlobMembers.MemberData member)
        {
            NameLabel.Text = member.Name;
            var type = Type.GetType(member.Type);

            _bottomLabel.Text = $"Type: {member.TypePretty}";
            ViewVariablesPropertyEditor editor;
            if (type == null)
            {
                // Type is server-side only.
                // Info whether it's reference or value type can be figured out from the sent value.
                if (member.Value is ViewVariablesBlobMembers.ServerValueTypeToken)
                {
                    // Value type, just display it stringified read-only.
                    editor = new ViewVariablesPropertyEditorDummy();
                }
                else
                {
                    // Has to be a reference type at this point.
                    DebugTools.Assert(member.Value is ViewVariablesBlobMembers.ReferenceToken || member.Value == null);
                    editor = _viewVariablesManager.PropertyFor(typeof(object));
                }
            }
            else
            {
                editor = _viewVariablesManager.PropertyFor(type);
            }

            var view = editor.Initialize(member.Value, !member.Editable);
            if (view.SizeFlagsHorizontal != SizeFlags.FillExpand)
            {
                NameLabel.SizeFlagsHorizontal = SizeFlags.FillExpand;
            }

            NameLabel.CustomMinimumSize = new Vector2(150, 0);
            TopContainer.AddChild(view);
            /*
            _beingEdited = obj;
            _editedProperty = propertyInfo;
            DebugTools.Assert(propertyInfo.DeclaringType != null);
            DebugTools.Assert(propertyInfo.DeclaringType.IsInstanceOfType(obj));

            var attr = propertyInfo.GetCustomAttribute<ViewVariablesAttribute>();
            DebugTools.Assert(attr != null);
            NameLabel.Text = propertyInfo.Name;

            _bottomLabel.Text = $"Type: {propertyInfo.PropertyType.FullName}";

            var editor = vvm.PropertyFor(propertyInfo.PropertyType);
            var value = propertyInfo.GetValue(obj);

            var view = editor.Initialize(value, attr.Access != VVAccess.ReadWrite);
            if (view.SizeFlagsHorizontal != SizeFlags.FillExpand)
            {
                NameLabel.SizeFlagsHorizontal = SizeFlags.FillExpand;
            }
            NameLabel.CustomMinimumSize = new Vector2(150, 0);
            TopContainer.AddChild(view);
            editor.OnValueChanged += v => { propertyInfo.SetValue(obj, v); };
            */
            return editor;
        }

        public void SetStyle(bool other)
        {
            PanelOverride = GetAlternatingStyleBox(other);
        }

        public static StyleBox GetAlternatingStyleBox(bool other)
        {
            var box = new StyleBoxFlat();
            box.BackgroundColor = other ? Color.Transparent : Color.Black.WithAlpha(0.25f);
            box.SetContentMargin(StyleBox.Margin.Vertical, 1);
            box.SetContentMargin(StyleBox.Margin.Horizontal, 5);
            return box;
        }

        protected override void MouseDown(GUIMouseButtonEventArgs args)
        {
            base.MouseDown(args);

            BottomContainer.Visible = !BottomContainer.Visible;
            args.Handle();
        }
    }
}
