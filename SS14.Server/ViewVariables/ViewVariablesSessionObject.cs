using System.Linq;
using System.Reflection;
using SS14.Shared.Network;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace SS14.Server.ViewVariables
{
    public class ViewVariablesSessionObject : ViewVariablesSession
    {
        public ViewVariablesSessionObject(NetSessionId playerSession, object o, uint sessionId) : base(playerSession, o, sessionId)
        {
        }

        public override ViewVariablesBlob DataRequest()
        {
            var blob = new ViewVariablesBlob
            {
                ObjectType = Object.GetType().AssemblyQualifiedName,
                ObjectTypePretty = Object.GetType().ToString(),
                Stringified = Object.ToString()
            };

            foreach (var property in ObjectType.GetProperties(BindingFlags.Public |
                                                        BindingFlags.FlattenHierarchy |
                                                        BindingFlags.Instance).OrderBy(p => p.Name))
            {
                var attr = property.GetCustomAttribute<ViewVariablesAttribute>();
                if (attr == null)
                {
                    continue;
                }

                var data = new ViewVariablesBlob.PropertyData
                {
                    Editable = attr.Access == VVAccess.ReadWrite,
                    Name = property.Name,
                    Type = property.PropertyType.AssemblyQualifiedName,
                    TypePretty = property.PropertyType.ToString(),
                };

                var value = property.GetValue(Object);

                if (value != null)
                {
                    var valType = value.GetType();
                    if (!valType.IsValueType)
                    {
                        // TODO: More flexibility in which types can be sent here.
                        if (valType != typeof(string))
                        {
                            value = new ViewVariablesBlob.ReferenceToken
                            {
                                Stringified = value.ToString()
                            };
                        }
                    }
                    else if (valType.IsServerSide())
                    {
                        // Can't send this value type over the wire.
                        value = new ViewVariablesBlob.ServerValueTypeToken
                        {
                            Stringified = value.ToString()
                        };
                    }
                }

                data.Value = value;
                blob.Properties.Add(data);
            }

            return blob;
        }
    }
}
