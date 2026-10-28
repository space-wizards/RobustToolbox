using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.ViewVariables.Editors;
using Robust.Shared.Input;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables
{
    internal sealed class ViewVariablesPropertyControl : PanelContainer
    {
        private readonly IClientViewVariablesManagerInternal _viewVariablesManager;
        private readonly IRobustSerializer _robustSerializer;

        private readonly CollapsibleHeading _heading = new();
        private readonly RichTextLabel _documentationLabel = new();
        private readonly RichTextLabel _typeLabel = new();

        public ViewVariablesPropertyControl(IClientViewVariablesManagerInternal viewVars, IRobustSerializer robustSerializer)
        {
            MouseFilter = MouseFilterMode.Pass;

            _viewVariablesManager = viewVars;
            _robustSerializer = robustSerializer;

            MouseFilter = MouseFilterMode.Pass;
            MinHeight = 25;

            var body = new CollapsibleBody();

            var container = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
            };

            var docLabel = new RichTextLabel();
            docLabel.SetMessage(FormattedMessage.FromMarkupOrThrow($"[bold]{Loc.GetString("view-variables-property-documentation")}[/bold]"), Color.White);
            container.AddChild(docLabel);

            container.AddChild(_documentationLabel);

            var typeLabel = new RichTextLabel();
            typeLabel.SetMessage(FormattedMessage.FromMarkupOrThrow($"[bold]{Loc.GetString("view-variables-property-type")}[/bold]"), Color.White);
            container.AddChild(typeLabel);

            container.AddChild(_typeLabel);
            body.AddChild(container);

            var collapsible = new Collapsible(_heading, body);
            AddChild(collapsible);
        }

        public void SetDocString(string docString)
        {
            _documentationLabel.SetMessage(docString, null, Color.White);
        }

        /// <param name="member">The member to show.</param>
        /// <param name="docStringKey">
        ///     Key to look up a local DocString with. Pass null for remote members and call
        ///     <see cref="SetDocString"/> once the DocString arrives from the server.
        /// </param>
        public VVPropEditor SetProperty(ViewVariablesBlobMembers.MemberData member, string? docStringKey)
        {
            _heading.Title = member.Name;
            var type = member.Value?.GetType();

            if (docStringKey != null)
                SetDocString(_viewVariablesManager.GetDocStringForFieldOrProperty(docStringKey));

            _typeLabel.SetMessage(member.TypePretty, Color.White);
            var editor = _viewVariablesManager.PropertyFor(type);

            var view = editor.Initialize(member.Value, !member.Editable);
            if (!view.HorizontalExpand)
            {
                _heading.Label.HorizontalExpand = true;
            }

            _heading.Label.MinSize = new Vector2(150, 0);
            _heading.Box.AddChild(view);
            /*
            _beingEdited = obj;
            _editedProperty = propertyInfo;
            DebugTools.Assert(propertyInfo.DeclaringType != null);
            DebugTools.Assert(propertyInfo.DeclaringType.IsInstanceOfType(obj));

            DebugTools.Assert(ViewVariablesUtility.TryGetViewVariablesAccess(fieldInfo, out var access));
            NameLabel.Text = propertyInfo.Name;

            _bottomLabel.Text = $"Type: {propertyInfo.PropertyType.FullName}";

            var editor = vvm.PropertyFor(propertyInfo.PropertyType);
            var value = propertyInfo.GetValue(obj);

            var view = editor.Initialize(value, access != VVAccess.ReadWrite);
            if (view.SizeFlagsHorizontal != SizeFlags.FillExpand)
            {
                NameLabel.HorizontalExpand = true;
            }
            NameLabel.MinSize = new Vector2(150, 0);
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
            box.SetContentMarginOverride(StyleBox.Margin.Vertical, 1);
            box.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);
            return box;
        }
    }
}
