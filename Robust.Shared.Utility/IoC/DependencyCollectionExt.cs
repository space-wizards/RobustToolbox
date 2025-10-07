using JetBrains.Annotations;

namespace Robust.Shared.IoC;

/// <summary>
/// Extension methods for <see cref="IDependencyCollection"/>.
/// </summary>
public static class DependencyCollectionExt
{
    /// <summary>
    /// Register a type as both implementation and interface.
    /// This is equivalent to calling <see cref="IDependencyCollection.Register{TInterface, TImplementation}(bool)"/> with both type args set to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="deps">The dependency collection to register into.</param>
    public static void Register<[MeansImplicitUse] T>(this IDependencyCollection deps)
        where T : class
    {
        deps.Register<T, T>();
    }
}
