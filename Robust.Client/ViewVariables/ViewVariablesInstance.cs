﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables
{
    /// <summary>
    ///     Controls the behavior of a VV window.
    /// </summary>
    internal abstract class ViewVariablesInstance
    {
        public readonly IClientViewVariablesManagerInternal ViewVariablesManager;
        protected readonly IRobustSerializer _robustSerializer;

        protected ViewVariablesInstance(IClientViewVariablesManagerInternal vvm, IRobustSerializer robustSerializer)
        {
            ViewVariablesManager = vvm;
            _robustSerializer = robustSerializer;
        }

        /// <summary>
        ///     Initializes this instance to work on a local object.
        /// </summary>
        /// <param name="window">The window to initialize by adding GUI components.</param>
        /// <param name="obj">The object that is being VV'd</param>
        public abstract void Initialize(DefaultWindow window, object obj);

        /// <summary>
        ///     Initializes this instance to work on a remote object.
        ///     This is called when the view variables manager has already made a session to the remote object.
        /// </summary>
        /// <param name="window">The window to initialize by adding GUI components.</param>
        /// <param name="blob">The data blob sent by the server for this remote object.</param>
        /// <param name="session">The session connecting to the remote object.</param>
        public virtual void Initialize(DefaultWindow window, ViewVariablesBlobMetadata blob, ViewVariablesRemoteSession session)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Invoked to "clean up" the instance, such as closing remote sessions.
        /// </summary>
        public virtual void Close()
        {
        }

        protected internal static IEnumerable<IGrouping<Type, Control>> LocalPropertyList(object obj, IClientViewVariablesManagerInternal vvm,
            IRobustSerializer robustSerializer)
        {
            var styleOther = false;
            var type = obj.GetType();

            var members = new List<(MemberInfo, VVAccess, object? value, Action<object?, bool> onValueChanged, Type)>();

            foreach (var fieldInfo in type.GetAllFields())
            {
                if (!ViewVariablesUtility.TryGetViewVariablesAccess(fieldInfo, out var access))
                {
                    continue;
                }

                members.Add((fieldInfo, (VVAccess)access, fieldInfo.GetValue(obj), (v, _) => fieldInfo.SetValue(obj, v),
                    fieldInfo.FieldType));
            }

            foreach (var propertyInfo in type.GetAllProperties())
            {
                if (!ViewVariablesUtility.TryGetViewVariablesAccess(propertyInfo, out var access))
                {
                    continue;
                }

                if (!propertyInfo.IsBasePropertyDefinition())
                {
                    continue;
                }

                members.Add((propertyInfo, (VVAccess)access, propertyInfo.GetValue(obj),
                    (v, _) => propertyInfo.GetSetMethod(true)!.Invoke(obj, new[] {v}), propertyInfo.PropertyType));
            }

            var groupedSorted = members
                .OrderBy(p => p.Item1.Name)
                .GroupBy(p => p.Item1.DeclaringType!, tuple =>
                {
                    var (memberInfo, access, value, onValueChanged, memberType) = tuple;
                    var data = new ViewVariablesBlobMembers.MemberData
                    {
                        Editable = access == VVAccess.ReadWrite,
                        Name = memberInfo.Name,
                        Type = memberType.AssemblyQualifiedName,
                        TypePretty = PrettyPrint.PrintUserFacingTypeShort(memberType, 2),
                        Value = value
                    };

                    string typeName = type.AssemblyQualifiedName!.Split(", ")[0];
                    string fullName = $"{typeName}.{memberInfo.Name}";
                    
                    var propertyEdit = new ViewVariablesPropertyControl(vvm, robustSerializer);
                    propertyEdit.SetStyle(styleOther = !styleOther);
                    var editor = propertyEdit.SetProperty(data, fullName);
                    editor.OnValueChanged += onValueChanged;
                    return propertyEdit;
                })
                .OrderByDescending(p => p.Key, TypeHelpers.TypeInheritanceComparer);

            return groupedSorted;
        }

        protected static Control MakeTopBar(string top, string middle, string bottom)
        {
            if (top == bottom)
            {
                return new Label {Text = top, ClipText = true};
            }

            //var smallFont =
            //    new VectorFont(IoCManager.Resolve<IResourceCache>().GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"),
            //        10);

            var headBox = new BoxContainer {
                Orientation = LayoutOrientation.Vertical,
                SeparationOverride = 0
            };

            headBox.AddChild(new Label {
                Text = top,
                ClipText = true
            });

            headBox.AddChild(new Label {
                Text = middle,
                FontColorOverride = Color.DarkGray,
                ClipText = true
            });

            headBox.AddChild(new Label {
                Text = bottom,
                FontColorOverride = Color.DarkGray,
                ClipText = true
            });

            return headBox;
        }
    }
}
