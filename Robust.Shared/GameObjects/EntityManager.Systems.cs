using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    [Pure]
    public T System<T>() where T : IEntitySystem
    {
        return _entitySystemManager.GetEntitySystem<T>();
    }

    [Pure]
    public T? SystemOrNull<T>() where T : IEntitySystem
    {
        return _entitySystemManager.GetEntitySystemOrNull<T>();
    }

    public bool TrySystem<T>([NotNullWhen(true)] out T? entitySystem) where T : IEntitySystem
    {
        return _entitySystemManager.TryGetEntitySystem(out entitySystem);
    }
}
