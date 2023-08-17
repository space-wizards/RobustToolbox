using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.Serialization;

public interface ISerializationGenerated<T> : ISerializationGenerated
{
    public void Copy(ref T target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null);

    public T Instantiate();
}

public interface ISerializationGenerated
{
}
