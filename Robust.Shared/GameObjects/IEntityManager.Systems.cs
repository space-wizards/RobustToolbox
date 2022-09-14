using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    /// <summary>
    /// Get an entity system of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of entity system to find.</typeparam>
    /// <returns>The <see cref="IEntitySystem"/> instance matching the specified type.</returns>
    T System<T>() where T : IEntitySystem;

    /// <summary>
    /// Get an entity system of the specified type, or null if it is not registered.
    /// </summary>
    /// <typeparam name="T">The type of entity system to find.</typeparam>
    /// <returns>The <see cref="IEntitySystem"/> instance matching the specified type, or null.</returns>
    T? SystemOrNull<T>() where T : IEntitySystem;

    /// <summary>
    /// Tries to get an entity system of the specified type.
    /// </summary>
    /// <typeparam name="T">Type of entity system to find.</typeparam>
    /// <param name="entitySystem">instance matching the specified type (if exists).</param>
    /// <returns>If an instance of the specified entity system type exists.</returns>
    bool TrySystem<T>([NotNullWhen(true)] out T? entitySystem) where T : IEntitySystem;
}
