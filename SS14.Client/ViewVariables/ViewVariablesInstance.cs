using System;
using System.Collections.Generic;
using System.Linq;
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
using SS14.Shared.ViewVariables;

namespace SS14.Client.ViewVariables
{
    /// <summary>
    ///     Controls the behavior of a VV window.
    /// </summary>
    internal abstract class ViewVariablesInstance
    {
        protected readonly IViewVariablesManagerInternal ViewVariablesManager;

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
        public virtual void Initialize(SS14Window window, ViewVariablesBlob blob, ViewVariablesRemoteSession session)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Invoked to "clean up" the instance, such as closing remote sessions.
        /// </summary>
        public virtual void Close()
        {
        }

        /// <summary>
        ///     Gets a property data object for a local object's <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="obj">The object owning the property.</param>
        /// <param name="info">The <see cref="PropertyInfo"/> describing which property it is.</param>
        protected static ViewVariablesBlob.PropertyData DataForProperty(object obj, PropertyInfo info)
        {
            var attr = info.GetCustomAttribute<ViewVariablesAttribute>();
            if (attr == null)
            {
                return null;
            }

            return new ViewVariablesBlob.PropertyData
            {
                Editable = attr.Access == VVAccess.ReadWrite,
                Name = info.Name,
                Type = info.PropertyType.AssemblyQualifiedName,
                TypePretty = info.PropertyType.ToString(),
                // AHEM.
                // Don't make this value be a reference token.
                // If you do, be aware that the instances do NOT expect this.
                Value = info.GetValue(obj)
            };
        }

        protected static IEnumerable<Control> LocalPropertyList(object obj, IViewVariablesManagerInternal vvm)
        {
            var styleOther = false;
            var type = obj.GetType();
            foreach (var property in type.GetProperties(BindingFlags.Public |
                                                        BindingFlags.FlattenHierarchy |
                                                        BindingFlags.Instance).OrderBy(p => p.Name))
            {
                var data = DataForProperty(obj, property);
                if (data == null)
                {
                    continue;
                }

                var propertyEdit = new ViewVariablesPropertyControl();
                propertyEdit.SetStyle(styleOther = !styleOther);
                var editor = propertyEdit.SetProperty(data);
                editor.OnValueChanged += o => property.SetValue(obj, o);
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
