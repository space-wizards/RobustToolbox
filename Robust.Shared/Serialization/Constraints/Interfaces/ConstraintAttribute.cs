using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Constraints.Attributes;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.Serialization.Constraints.Interfaces;

public abstract class ConstraintAttribute : Attribute
{
    public abstract Type[] SupportedTypes { get; }

    public abstract bool Validate(ISerializationManager serializationManager, object? value,
        IDependencyCollection dependencies, ISerializationContext? context = null);
}
