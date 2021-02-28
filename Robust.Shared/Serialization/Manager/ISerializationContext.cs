using System;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager
{
    //todo paul make this actually usable
    //todo paul allow generics
    public interface ISerializationContext
    {
        Dictionary<(Type, Type), object> TypeReaders { get; }
        Dictionary<Type, object> TypeWriters { get; }
        Dictionary<Type, object> TypeCopiers { get; }
    }
}
