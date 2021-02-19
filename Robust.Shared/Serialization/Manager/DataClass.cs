using System;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Serialization.Manager
{
    [ImplicitDataDefinitionForInheritors]
    public abstract class DataClass
    {
        public T? GetValue<T>(string name)
        {
            if(IoCManager.Resolve<IServ3Manager>().TryGetDataClassField(this, name, out T? value))
            {
                return value;
            }

            throw new ArgumentException("Cannot find supplied name.", nameof(name));
        }
    }
}
