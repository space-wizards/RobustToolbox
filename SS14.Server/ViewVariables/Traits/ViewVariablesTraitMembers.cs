using System;
using System.Collections.Generic;
using System.Reflection;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace SS14.Server.ViewVariables.Traits
{
    internal class ViewVariablesTraitMembers : ViewVariablesTrait
    {
        private readonly List<MemberInfo> _members = new List<MemberInfo>();

        public ViewVariablesTraitMembers(ViewVariablesSession session) : base(session)
        {
        }

        public override ViewVariablesBlob DataRequest(ViewVariablesRequest messageRequestMeta)
        {
            if (!(messageRequestMeta is ViewVariablesRequestMembers))
            {
                return null;
            }

            var dataList = new List<ViewVariablesBlobMembers.PropertyData>();
            var blob = new ViewVariablesBlobMembers
            {
                Properties = dataList
            };

            foreach (var property in Session.ObjectType.GetAllProperties())
            {
                var attr = property.GetCustomAttribute<ViewVariablesAttribute>();
                if (attr == null)
                {
                    continue;
                }

                dataList.Add(new ViewVariablesBlobMembers.PropertyData
                {
                    Editable = attr.Access == VVAccess.ReadWrite,
                    Name = property.Name,
                    Type = property.PropertyType.AssemblyQualifiedName,
                    TypePretty = property.PropertyType.ToString(),
                    Value = property.GetValue(Session.Object),
                    PropertyIndex = _members.Count
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

                dataList.Add(new ViewVariablesBlobMembers.PropertyData
                {
                    Editable = attr.Access == VVAccess.ReadWrite,
                    Name = field.Name,
                    Type = field.FieldType.AssemblyQualifiedName,
                    TypePretty = field.FieldType.ToString(),
                    Value = field.GetValue(Session.Object),
                    PropertyIndex = _members.Count
                });
                _members.Add(field);
            }

            dataList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            foreach (var data in dataList)
            {
                var value = data.Value;
                if (value != null)
                {
                    var valType = value.GetType();
                    if (!valType.IsValueType)
                    {
                        // TODO: More flexibility in which types can be sent here.
                        if (valType != typeof(string))
                        {
                            value = new ViewVariablesBlobMembers.ReferenceToken
                            {
                                Stringified = value.ToString()
                            };
                        }
                    }
                    else if (valType.IsServerSide())
                    {
                        // Can't send this value type over the wire.
                        value = new ViewVariablesBlobMembers.ServerValueTypeToken
                        {
                            Stringified = value.ToString()
                        };
                    }
                }

                data.Value = value;
            }

            return blob;
        }

        public override bool TryGetRelativeObject(object property, out object value)
        {
            if (property is ViewVariablesPropertySelector selector)
            {
                if (selector.Index > _members.Count)
                {
                    value = default(object);
                    return false;
                }

                var member = _members[selector.Index];
                if (member is PropertyInfo propertyInfo)
                {
                    try
                    {
                        value = propertyInfo.GetValue(Session.Object);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("vv", "Exception while getting property {0} on session {1} object {2}: {3}",
                            propertyInfo.Name, Session.SessionId, Session.Object, e);
                        value = default(object);
                        return false;
                    }
                }
                else if (member is FieldInfo field)
                {
                    try
                    {
                        value = field.GetValue(Session.Object);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("vv", "Exception while modifying field {0} on session {1} object {2}: {3}",
                            field.Name, Session.SessionId, Session.Object, e);
                        value = default(object);
                        return false;
                    }
                }

                throw new InvalidOperationException();
            }

            return base.TryGetRelativeObject(property, out value);
        }

        public override bool TryModifyProperty(object[] property, object value)
        {
            if (property[0] is ViewVariablesPropertySelector selector)
            {
                if (selector.Index >= _members.Count)
                {
                    return false;
                }

                var member = _members[selector.Index];
                if (member is PropertyInfo propertyInfo)
                {
                    try
                    {
                        propertyInfo.GetSetMethod(true).Invoke(Session.Object, new[] {value});
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("vv", "Exception while modifying property {0} on session {1} object {2}: {3}",
                            propertyInfo.Name, Session.SessionId, Session.Object, e);
                        return false;
                    }
                }

                if (member is FieldInfo field)
                {
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
                }

                throw new InvalidOperationException();
            }

            return base.TryModifyProperty(property, value);
        }
    }
}
