using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables.Traits
{
    internal sealed class ViewVariablesTraitMembers : ViewVariablesTrait
    {
        private readonly List<MemberInfo> _members = new List<MemberInfo>();

        public ViewVariablesTraitMembers(ViewVariablesSession session) : base(session)
        {
        }

        public override ViewVariablesBlob? DataRequest(ViewVariablesRequest messageRequestMeta)
        {
            if (!(messageRequestMeta is ViewVariablesRequestMembers))
            {
                return null;
            }

            var dataList = new List<ViewVariablesBlobMembers.MemberData>();
            var blob = new ViewVariablesBlobMembers
            {
                Members = dataList
            };

            foreach (var property in Session.ObjectType.GetAllProperties())
            {
                var attr = property.GetCustomAttribute<ViewVariablesAttribute>();
                if (attr == null)
                {
                    continue;
                }

                if (!property.IsBasePropertyDefinition())
                {
                    continue;
                }

                var display = property.GetCustomAttribute<ViewVariablesNumericAttribute>();

                dataList.Add(new ViewVariablesBlobMembers.MemberData
                {
                    Editable = attr.Access == VVAccess.ReadWrite,
                    Name = property.Name,
                    Type = property.PropertyType.AssemblyQualifiedName,
                    TypePretty = TypeAbbreviation.Abbreviate(property.PropertyType),
                    Value = property.GetValue(Session.Object),
                    PropertyIndex = _members.Count,
                    Display = display?.DisplayMethod ?? NumericDisplay.None
                });
                _members.Add(property);
            }

            foreach (var field in Session.ObjectType.GetAllFields())
            {
                var attr = field.GetCustomAttribute<ViewVariablesAttribute>();
                if (attr == null)
                {
                    continue;
                }

                var display = field.GetCustomAttribute<ViewVariablesNumericAttribute>();

                dataList.Add(new ViewVariablesBlobMembers.MemberData
                {
                    Editable = attr.Access == VVAccess.ReadWrite,
                    Name = field.Name,
                    Type = field.FieldType.AssemblyQualifiedName,
                    TypePretty = TypeAbbreviation.Abbreviate(field.FieldType),
                    Value = field.GetValue(Session.Object),
                    PropertyIndex = _members.Count,
                    Display = display?.DisplayMethod ?? NumericDisplay.None
                });
                _members.Add(field);
            }

            dataList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            foreach (var data in dataList)
            {
                data.Value = MakeValueNetSafe(data.Value);
            }

            return blob;
        }

        public override bool TryGetRelativeObject(object property, out object? value)
        {
            if (!(property is ViewVariablesMemberSelector selector))
            {
                return base.TryGetRelativeObject(property, out value);
            }

            if (selector.Index > _members.Count)
            {
                value = default;
                return false;
            }

            var member = _members[selector.Index];
            switch (member)
            {
                case PropertyInfo propertyInfo:
                    try
                    {
                        value = propertyInfo.GetValue(Session.Object);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("vv", "Exception while getting property {0} on session {1} object {2}: {3}",
                            propertyInfo.Name, Session.SessionId, Session.Object, e);
                        value = default;
                        return false;
                    }

                case FieldInfo field:
                    try
                    {
                        value = field.GetValue(Session.Object);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("vv", "Exception while modifying field {0} on session {1} object {2}: {3}",
                            field.Name, Session.SessionId, Session.Object, e);
                        value = default;
                        return false;
                    }

                default:
                    throw new InvalidOperationException();
            }
        }

        public override bool TryModifyProperty(object[] property, object value)
        {
            if (!(property[0] is ViewVariablesMemberSelector selector))
            {
                return base.TryModifyProperty(property, value);
            }

            if (selector.Index >= _members.Count)
            {
                return false;
            }

            var member = _members[selector.Index];
            switch (member)
            {
                case PropertyInfo propertyInfo:
                    try
                    {
                        propertyInfo.GetSetMethod(true)!.Invoke(Session.Object, new[] {value});
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("vv", "Exception while modifying property {0} on session {1} object {2}: {3}",
                            propertyInfo.Name, Session.SessionId, Session.Object, e);
                        return false;
                    }

                case FieldInfo field:
                    try
                    {
                        field.SetValue(Session.Object, value);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("vv", "Exception while modifying field {0} on session {1} object {2}: {3}",
                            field.Name, Session.SessionId, Session.Object, e);
                        return false;
                    }

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
