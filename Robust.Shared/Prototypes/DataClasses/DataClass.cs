using System;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Robust.Shared.Prototypes.DataClasses
{
    public abstract class DataClass
    {
        public T GetValue<T>(string name)
        {
            if(IoCManager.Resolve<IDataClassManager>().TryGetDataClassField(this, name, out T? value))
            {
                return value;
            }

            throw new ArgumentException("Cannot find supplied name.", nameof(name));
        }
    }
}
