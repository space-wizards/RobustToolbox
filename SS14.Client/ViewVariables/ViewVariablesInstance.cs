using System;
using System.Collections.Generic;
using System.Reflection;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Client.ViewVariables.Editors;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace SS14.Client.ViewVariables
{
    /// <summary>
    ///     Controls the behavior of a VV window.
    /// </summary>
    internal abstract class ViewVariablesInstance
    {
        public readonly IViewVariablesManagerInternal ViewVariablesManager;

        protected ViewVariablesInstance(IViewVariablesManagerInternal vvm)
        {
            ViewVariablesManager = vvm;
        }

        /// <summary>
        ///     Initializes this instance to work on a local object.
        /// </summary>
        /// <param name="window">The window to initialize by adding GUI components.</param>
        /// <param name="obj">The object that is being VV'd</param>
        public abstract void Initialize(SS14Window window, object obj);

        /// <summary>
        ///     Initializes this instance to work on a remote object.
        ///     This is called when the view variables manager has already made a session to the remote object.
        /// </summary>
        /// <param name="window">The window to initialize by adding GUI components.</param>
        /// <param name="blob">The data blob sent by the server for this remote object.</param>
        /// <param name="session">The session connecting to the remote object.</param>
        public virtual void Initialize(SS14Window window, ViewVariablesBlobMetadata blob, ViewVariablesRemoteSession session)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Invoked to "clean up" the instance, such as closing remote sessions.
        /// </summary>
        public virtual void Close()
        {
        }

        protected internal static IEnumerable<Control> LocalPropertyList(object obj, IViewVariablesManagerInternal vvm)
        {
            var styleOther = false;
            var type = obj.GetType();

            var members = new List<(MemberInfo, VVAccess, object value, Action<object> onValueChanged, Type)>();

            foreach (var fieldInfo in type.GetAllFields())
            {
                var attr = fieldInfo.GetCustomAttribute<ViewVariablesAttribute>();
                if (attr == null)
                {
                    continue;
                }

                members.Add((fieldInfo, attr.Access, fieldInfo.GetValue(obj), v => fieldInfo.SetValue(obj, v),
                    fieldInfo.FieldType));
            }

            foreach (var propertyInfo in type.GetAllProperties())
            {
                var attr = propertyInfo.GetCustomAttribute<ViewVariablesAttribute>();
                if (attr == null)
                {
                    continue;
                }

                members.Add((propertyInfo, attr.Access, propertyInfo.GetValue(obj),
                    v => propertyInfo.GetSetMethod(true).Invoke(obj, new[] {v}), propertyInfo.PropertyType));
            }

            members.Sort((a, b) => string.Compare(a.Item1.Name, b.Item1.Name, StringComparison.Ordinal));

            foreach (var (memberInfo, access, value, onValueChanged, memberType) in members)
            {
                var data = new ViewVariablesBlobMembers.MemberData
                {
                    Editable = access == VVAccess.ReadWrite,
                    Name = memberInfo.Name,
                    Type = memberType.AssemblyQualifiedName,
                    TypePretty = memberType.ToString(),
                    Value = value
                };

                var propertyEdit = new ViewVariablesPropertyControl();
                propertyEdit.SetStyle(styleOther = !styleOther);
                var editor = propertyEdit.SetProperty(data);
                editor.OnValueChanged += onValueChanged;
                // TODO: should this maybe not be hardcoded?
                if (editor is ViewVariablesPropertyEditorReference refEditor)
                {
                    refEditor.OnPressed += () => vvm.OpenVV(data.Value);
                }

                yield return propertyEdit;
            }
        }

        protected static Control MakeTopBar(string top, string bottom)
        {
            if (top == bottom)
            {
                return new Label {Text = top};
            }

            var smallFont =
                new VectorFont(IoCManager.Resolve<IResourceCache>().GetResource<FontResource>("/Fonts/CALIBRI.TTF"))
                {
                    Size = 10
                };

            // Custom ToString() implementation.
            var headBox = new VBoxContainer {SeparationOverride = 0};
            headBox.AddChild(new Label {Text = top});
            headBox.AddChild(new Label {Text = bottom, FontOverride = smallFont, FontColorOverride = Color.DarkGray});
            return headBox;
        }
    }
}
