namespace Robust.Shared.GameObjects;

public static class EntityExt
{
    /// <summary>
    ///     Converts an Entity{T} to Entity{T?}, making it compatible with methods that take nullable components.
    /// </summary>
    /// <typeparam name="T">The component type. Must implement IComponent.</typeparam>
    /// <param name="ent">The source entity to convert.</param>
    /// <returns>An Entity{T?} with the same owner and component as the source entity.</returns>
    /// <example>
    ///     <code>
    /// // Instead of:
    /// Entity{MyComponent?} nullable = (ent, ent.Comp);
    ///
    /// // You can write:
    /// Entity{MyComponent?} nullable = ent.AsNullable();
    /// </code>
    /// </example>
    public static Entity<T?> AsNullable<T>(this Entity<T> ent) where T : IComponent
    {
        return new(ent.Owner, ent.Comp);
    }

    /// <inheritdoc cref="AsNullable{T}" />
    public static Entity<T1?, T2?> AsNullable<T1, T2>(this Entity<T1, T2> ent)
        where T1 : IComponent
        where T2 : IComponent
    {
        return new(ent.Owner, ent.Comp1, ent.Comp2);
    }

    /// <inheritdoc cref="AsNullable{T}" />
    public static Entity<T1?, T2?, T3?> AsNullable<T1, T2, T3>(this Entity<T1, T2, T3> ent)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        return new(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3);
    }

    /// <inheritdoc cref="AsNullable{T}" />
    public static Entity<T1?, T2?, T3?, T4?> AsNullable<T1, T2, T3, T4>(this Entity<T1, T2, T3, T4> ent)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        return new(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4);
    }

    /// <inheritdoc cref="AsNullable{T}" />
    public static Entity<T1?, T2?, T3?, T4?, T5?> AsNullable<T1, T2, T3, T4, T5>(this Entity<T1, T2, T3, T4, T5> ent)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        return new(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5);
    }

    /// <inheritdoc cref="AsNullable{T}" />
    public static Entity<T1?, T2?, T3?, T4?, T5?, T6?> AsNullable<T1, T2, T3, T4, T5, T6>(
        this Entity<T1, T2, T3, T4, T5, T6> ent)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        return new(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, ent.Comp6);
    }

    /// <inheritdoc cref="AsNullable{T}" />
    public static Entity<T1?, T2?, T3?, T4?, T5?, T6?, T7?> AsNullable<T1, T2, T3, T4, T5, T6, T7>(
        this Entity<T1, T2, T3, T4, T5, T6, T7> ent)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        return new(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, ent.Comp6, ent.Comp7);
    }

    /// <inheritdoc cref="AsNullable{T}" />
    public static Entity<T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?> AsNullable<T1, T2, T3, T4, T5, T6, T7, T8>(
        this Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
    {
        return new(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, ent.Comp6, ent.Comp7, ent.Comp8);
    }
}
