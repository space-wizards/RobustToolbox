using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.Serialization;

public interface ISerializationGenerated<T> : ISerializationGenerated
{
    public T Copy(ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null);
}

public interface ISerializationGenerated
{
    public object CopyObject(ISerializationManager serialization, SerializationHookContext hookCtx,
        ISerializationContext? context = null);
}
