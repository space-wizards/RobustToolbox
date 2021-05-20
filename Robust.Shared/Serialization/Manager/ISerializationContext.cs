using System;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager
{
    // TODO Serialization: make this actually not kanser to use holy moly (& allow generics)
    public interface ISerializationContext
    {
        Dictionary<(Type, Type), object> TypeReaders { get; }
        Dictionary<Type, object> TypeWriters { get; }
        Dictionary<Type, object> TypeCopiers { get; }
        Dictionary<(Type, Type), object> TypeValidators { get; }
    }
}
