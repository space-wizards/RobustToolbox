using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Robust.Shared.ViewVariables.ViewVariablesBlobMembers;

namespace Robust.Server.ViewVariables.Traits
{
    internal sealed class ViewVariablesTraitMembers : ViewVariablesTrait
    {
        private readonly List<MemberInfo> _members = new();

        public ViewVariablesTraitMembers(IViewVariablesSession session) : base(session)
        {
        }

        public override ViewVariablesBlob? DataRequest(ViewVariablesRequest messageRequestMeta)
        {
            if (messageRequestMeta is ViewVariablesRequestMembers)
            {
                var members = new List<(MemberData mData, MemberInfo mInfo)>();

                foreach (var property in Session.ObjectType.GetAllProperties())
                {

                    if (!ViewVariablesUtility.TryGetViewVariablesAccess(property, out var access))
                    {
                        continue;
                    }

                    if (!property.IsBasePropertyDefinition())
                    {
                        continue;
                    }

                    members.Add((new MemberData
                    {
                        Editable = access == VVAccess.ReadWrite,
                        Name = property.Name,
                        Type = property.PropertyType.AssemblyQualifiedName,
                        TypePretty = TypeAbbreviation.Abbreviate(property.PropertyType),
                        Value = property.GetValue(Session.Object),
                        PropertyIndex = _members.Count
                    }, property));
                    _members.Add(property);
                }

                foreach (var field in Session.ObjectType.GetAllFields())
                {
                    if (!ViewVariablesUtility.TryGetViewVariablesAccess(field, out var access))
                    {
                        continue;
                    }

                    members.Add((new MemberData
                    {
                        Editable = access == VVAccess.ReadWrite,
                        Name = field.Name,
                        Type = field.FieldType.AssemblyQualifiedName,
                        TypePretty = TypeAbbreviation.Abbreviate(field.FieldType),
                        Value = field.GetValue(Session.Object),
                        PropertyIndex = _members.Count
                    }, field));

                    _members.Add(field);
                }

                foreach (var (mData, mInfo) in members)
                {
                    mData.Value = MakeValueNetSafe(mData.Value) ?? MakeNullValueNetSafe(mInfo.GetUnderlyingType());
                }

                var dataList = members
                    .OrderBy(p => p.mData.Name)
                    .GroupBy(p => p.mInfo.DeclaringType!)
                    .OrderByDescending(g => g.Key, TypeHelpers.TypeInheritanceComparer)
                    .Select(g =>
                    (
                        TypeAbbreviation.Abbreviate(g.Key),
                        g.Select(d => d.mData).ToList()
                    ))
                    .ToList();

                return new ViewVariablesBlobMembers
                {
                    MemberGroups = dataList
                };
            }

            if (messageRequestMeta is ViewVariablesRequestAllPrototypes protoReq)
            {
                var list = new List<string>();

                foreach (var prototype in IoCManager.Resolve<IPrototypeManager>().EnumeratePrototypes(protoReq.Variant))
                {
                    list.Add(prototype.ID);
                }

                return new ViewVariablesBlobAllPrototypes()
                {
                    Variant = protoReq.Variant,
                    Prototypes = list,
                };
            }

            return null;
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
