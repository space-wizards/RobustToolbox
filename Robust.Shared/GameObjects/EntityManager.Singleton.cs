using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    // REMARK: No API that allows you to use these queries without them throwing over non-uniqueness should be added.
    //         It's a pretty simple, natural error condition and the game *should* yell about it.

    /// <summary>
    ///     Gets the sole entity with the given component.
    /// </summary>
    /// <typeparam name="TComp1">The component to look for as a tag.</typeparam>
    /// <returns>The singleton entity.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request.</exception>
    public Entity<TComp1> Single<TComp1>()
        where TComp1: IComponent
    {
        var index = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];

        if (index.Keys.FirstOrNull() is { } ent && index.Count == 1)
        {
            return new Entity<TComp1>(ent, (TComp1)index[ent]);
        }

        if (index.Count > 1)
        {
            throw new NonUniqueSingletonException(index.Keys.ToArray(), typeof(TComp1));
        }
        else
        {
            // 0.
            throw new MatchNotFoundException(typeof(TComp1));
        }
    }

    /// <summary>
    ///     Gets the sole entity with the given components.
    /// </summary>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <returns>The singleton entity.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public Entity<TComp1, TComp2> Single<TComp1, TComp2>()
        where TComp1: IComponent
        where TComp2: IComponent
    {
        var query = EntityQueryEnumerator<TComp1, TComp2>();

        if (!query.MoveNext(out var ent, out var comp1, out var comp2))
            throw new MatchNotFoundException(typeof(TComp1), typeof(TComp2));

        if (query.MoveNext(out var ent2, out _, out _))
        {
            var list = new List<EntityUid> { ent, ent2 };

            while (query.MoveNext(out var ent3, out _, out _))
            {
                list.Add(ent3);
            }

            throw new NonUniqueSingletonException(list.ToArray(), typeof(TComp1), typeof(TComp2));
        }

        return new(ent, comp1, comp2);
    }

    /// <summary>
    ///     Gets the sole entity with the given components.
    /// </summary>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <returns>The singleton entity.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public Entity<TComp1, TComp2, TComp3> Single<TComp1, TComp2, TComp3>()
        where TComp1: IComponent
        where TComp2: IComponent
        where TComp3: IComponent
    {
        var query = EntityQueryEnumerator<TComp1, TComp2, TComp3>();

        if (!query.MoveNext(out var ent, out var comp1, out var comp2, out var comp3))
            throw new MatchNotFoundException(typeof(TComp1), typeof(TComp2), typeof(TComp3));

        if (query.MoveNext(out var ent2, out _, out _, out _))
        {
            var list = new List<EntityUid> { ent, ent2 };

            while (query.MoveNext(out var ent3, out _, out _, out _))
            {
                list.Add(ent3);
            }

            throw new NonUniqueSingletonException(list.ToArray(), typeof(TComp1), typeof(TComp2), typeof(TComp3));
        }

        return new(ent, comp1, comp2, comp3);
    }

    /// <summary>
    ///     Gets the sole entity with the given components.
    /// </summary>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <typeparam name="TComp4">The third component to look for as a tag.</typeparam>
    /// <returns>The singleton entity.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public Entity<TComp1, TComp2, TComp3, TComp4> Single<TComp1, TComp2, TComp3, TComp4>()
        where TComp1: IComponent
        where TComp2: IComponent
        where TComp3: IComponent
        where TComp4: IComponent
    {
        var query = EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>();

        if (!query.MoveNext(out var ent, out var comp1, out var comp2, out var comp3, out var comp4))
            throw new MatchNotFoundException(typeof(TComp1), typeof(TComp2), typeof(TComp3), typeof(TComp4));

        if (query.MoveNext(out var ent2, out _, out _, out _, out _))
        {
            var list = new List<EntityUid> { ent, ent2 };

            while (query.MoveNext(out var ent3, out _, out _, out _, out _))
            {
                list.Add(ent3);
            }

            throw new NonUniqueSingletonException(list.ToArray(), typeof(TComp1), typeof(TComp2), typeof(TComp3), typeof(TComp4));
        }

        return new(ent, comp1, comp2, comp3, comp4);
    }

    /// <summary>
    ///     Gets the sole entity with the given component, if it exists. Still throws if there's more than one.
    /// </summary>
    /// <param name="entity">The singleton entity, if any.</param>
    /// <typeparam name="TComp1">The component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <returns>Success.</returns>
    public bool TrySingle<TComp1>([NotNullWhen(true)] out Entity<TComp1>? entity)
        where TComp1: IComponent
    {
        var index = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];

        if (index.Keys.FirstOrNull() is { } ent && index.Count == 1)
        {
            entity = new Entity<TComp1>(ent, (TComp1)index[ent]);
            return true;
        }
        else
        {
            entity = null;
        }

        if (index.Count > 1)
        {
            throw new NonUniqueSingletonException(index.Keys.ToArray(), typeof(TComp1));
        }

        return false;
    }

    /// <summary>
    ///     Gets the sole entity with the given components, if one exists, or returns if one does not.
    /// </summary>
    /// <param name="entity">The singleton entity, if any.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <returns>Success.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public bool TrySingle<TComp1, TComp2>([NotNullWhen(true)] out Entity<TComp1, TComp2>? entity)
        where TComp1: IComponent
        where TComp2: IComponent
    {
        var query = EntityQueryEnumerator<TComp1, TComp2>();

        if (!query.MoveNext(out var ent, out var comp1, out var comp2))
        {
            entity = null;
            return false;
        }

        if (query.MoveNext(out var ent2, out _, out _))
        {
            var list = new List<EntityUid> { ent, ent2 };

            while (query.MoveNext(out var ent3, out _, out _))
            {
                list.Add(ent3);
            }

            throw new NonUniqueSingletonException(list.ToArray(), typeof(TComp1), typeof(TComp2));
        }

        entity = new(ent, comp1, comp2);
        return true;
    }

    /// <summary>
    ///     Gets the sole entity with the given components, if one exists, or returns if one does not.
    /// </summary>
    /// <param name="entity">The singleton entity, if any.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <returns>Success.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public bool TrySingle<TComp1, TComp2, TComp3>([NotNullWhen(true)] out Entity<TComp1, TComp2, TComp3>? entity)
        where TComp1: IComponent
        where TComp2: IComponent
        where TComp3: IComponent
    {
        var query = EntityQueryEnumerator<TComp1, TComp2, TComp3>();

        if (!query.MoveNext(out var ent, out var comp1, out var comp2, out var comp3))
        {
            entity = null;
            return false;
        }

        if (query.MoveNext(out var ent2, out _, out _, out _))
        {
            var list = new List<EntityUid> { ent, ent2 };

            while (query.MoveNext(out var ent3, out _, out _, out _))
            {
                list.Add(ent3);
            }

            throw new NonUniqueSingletonException(list.ToArray(), typeof(TComp1), typeof(TComp2), typeof(TComp3));
        }

        entity = new(ent, comp1, comp2, comp3);
        return true;
    }

    /// <summary>
    ///     Gets the sole entity with the given components, if one exists, or returns if one does not.
    /// </summary>
    /// <param name="entity">The singleton entity, if any.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <typeparam name="TComp4">The fourth component to look for as a tag.</typeparam>
    /// <returns>Success.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public bool TrySingle<TComp1, TComp2, TComp3, TComp4>([NotNullWhen(true)] out Entity<TComp1, TComp2, TComp3, TComp4>? entity)
        where TComp1: IComponent
        where TComp2: IComponent
        where TComp3: IComponent
        where TComp4: IComponent
    {
        var query = EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>();

        if (!query.MoveNext(out var ent, out var comp1, out var comp2, out var comp3, out var comp4))
        {
            entity = null;
            return false;
        }

        if (query.MoveNext(out var ent2, out _, out _, out _, out _))
        {
            var list = new List<EntityUid> { ent, ent2 };

            while (query.MoveNext(out var ent3, out _, out _, out _, out _))
            {
                list.Add(ent3);
            }

            throw new NonUniqueSingletonException(list.ToArray(), typeof(TComp1), typeof(TComp2), typeof(TComp3), typeof(TComp4));
        }

        entity = new(ent, comp1, comp2, comp3, comp4);
        return true;
    }

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The fallback prototype to spawn on failure.</param>
    /// <param name="location">The location to spawn the singleton. Nullspace works.</param>
    /// <typeparam name="TComp1">The component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after spawning the fallback.</exception>
    /// <returns>The singleton entity.</returns>
    /// <remarks>
    ///     This does not return the entity it spawns, it tries to look it up, so if spawning that entity violates
    ///     singleton status (or it lacks the necessary component) this will throw immediately after instead of later
    ///     on next call.
    ///
    ///     This will also still throw <see cref="MatchNotFoundException"/> if the spawned entity doesn't match at all.
    /// </remarks>
    public Entity<TComp1> SingleOrSpawn<TComp1>(EntProtoId fallback, MapCoordinates location)
        where TComp1: IComponent
    {
        if (TrySingle<TComp1>(out var ent))
            return ent.Value;

        Spawn(fallback, location);

        return Single<TComp1>();
    }

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The fallback prototype to spawn on failure.</param>
    /// <param name="location">The location to spawn the singleton. Nullspace works.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after spawning the fallback.</exception>
    /// <returns>The singleton entity.</returns>
    /// <remarks>
    ///     This does not return the entity it spawns, it tries to look it up, so if spawning that entity violates
    ///     singleton status (or it lacks the necessary component) this will throw immediately after instead of later
    ///     on next call.
    ///
    ///     This will also still throw <see cref="MatchNotFoundException"/> if the spawned entity doesn't match at all.
    /// </remarks>
    public Entity<TComp1, TComp2> SingleOrSpawn<TComp1, TComp2>(EntProtoId fallback, MapCoordinates location)
        where TComp1: IComponent
        where TComp2: IComponent
    {
        if (TrySingle<TComp1, TComp2>(out var ent))
            return ent.Value;

        Spawn(fallback, location);

        return Single<TComp1, TComp2>();
    }

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The fallback prototype to spawn on failure.</param>
    /// <param name="location">The location to spawn the singleton. Nullspace works.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after spawning the fallback.</exception>
    /// <returns>The singleton entity.</returns>
    /// <remarks>
    ///     This does not return the entity it spawns, it tries to look it up, so if spawning that entity violates
    ///     singleton status (or it lacks the necessary component) this will throw immediately after instead of later
    ///     on next call.
    ///
    ///     This will also still throw <see cref="MatchNotFoundException"/> if the spawned entity doesn't match at all.
    /// </remarks>
    public Entity<TComp1, TComp2, TComp3> SingleOrSpawn<TComp1, TComp2, TComp3>(EntProtoId fallback, MapCoordinates location)
        where TComp1: IComponent
        where TComp2: IComponent
        where TComp3: IComponent
    {
        if (TrySingle<TComp1, TComp2, TComp3>(out var ent))
            return ent.Value;

        Spawn(fallback, location);

        return Single<TComp1, TComp2, TComp3>();
    }

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The fallback prototype to spawn on failure.</param>
    /// <param name="location">The location to spawn the singleton. Nullspace works.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <typeparam name="TComp4">The fourth component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after spawning the fallback.</exception>
    /// <returns>The singleton entity.</returns>
    /// <remarks>
    ///     This does not return the entity it spawns, it tries to look it up, so if spawning that entity violates
    ///     singleton status (or it lacks the necessary component) this will throw immediately after instead of later
    ///     on next call.
    ///
    ///     This will also still throw <see cref="MatchNotFoundException"/> if the spawned entity doesn't match at all.
    /// </remarks>
    public Entity<TComp1, TComp2, TComp3, TComp4> SingleOrSpawn<TComp1, TComp2, TComp3, TComp4>(EntProtoId fallback, MapCoordinates location)
        where TComp1: IComponent
        where TComp2: IComponent
        where TComp3: IComponent
        where TComp4: IComponent
    {
        if (TrySingle<TComp1, TComp2, TComp3, TComp4>(out var ent))
            return ent.Value;

        Spawn(fallback, location);

        return Single<TComp1, TComp2, TComp3, TComp4>();
    }

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The action to call on fallback, should no match exist.</param>
    /// <typeparam name="TComp1">The component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after calling fallback.</exception>
    /// <returns>The singleton entity.</returns>
    public Entity<TComp1> SingleOrInit<TComp1>(Action fallback)
        where TComp1: IComponent
    {
        if (TrySingle<TComp1>(out var ent))
            return ent.Value;

        fallback();

        return Single<TComp1>();
    }

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The action to call on fallback, should no match exist.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after calling fallback.</exception>
    /// <returns>The singleton entity.</returns>
    public Entity<TComp1, TComp2> SingleOrInit<TComp1, TComp2>(Action fallback)
        where TComp1: IComponent
        where TComp2: IComponent
    {
        if (TrySingle<TComp1, TComp2>(out var ent))
            return ent.Value;

        fallback();

        return Single<TComp1, TComp2>();
    }

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The action to call on fallback, should no match exist.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after calling fallback.</exception>
    /// <returns>The singleton entity.</returns>
    public Entity<TComp1, TComp2, TComp3> SingleOrInit<TComp1, TComp2, TComp3>(Action fallback)
        where TComp1: IComponent
        where TComp2: IComponent
        where TComp3: IComponent
    {
        if (TrySingle<TComp1, TComp2, TComp3>(out var ent))
            return ent.Value;

        fallback();

        return Single<TComp1, TComp2, TComp3>();
    }

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The action to call on fallback, should no match exist.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <typeparam name="TComp4">The fourth component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after calling fallback.</exception>
    /// <returns>The singleton entity.</returns>
    public Entity<TComp1, TComp2, TComp3, TComp4> SingleOrInit<TComp1, TComp2, TComp3, TComp4>(Action fallback)
        where TComp1: IComponent
        where TComp2: IComponent
        where TComp3: IComponent
        where TComp4: IComponent
    {
        if (TrySingle<TComp1, TComp2, TComp3, TComp4>(out var ent))
            return ent.Value;

        fallback();

        return Single<TComp1, TComp2, TComp3, TComp4>();
    }
}

/// <summary>
///     Exception for when <see cref="float"/> and co cannot find a unique match.
/// </summary>
/// <param name="matches">The set of matching entities.</param>
/// <param name="components">The set of components you tried to match over.</param>
public sealed class NonUniqueSingletonException(EntityUid[] matches, params Type[] components) : Exception
{
    public override string Message =>
        $"Expected precisely one entity to match the component set {string.Join(", ", components)}, but found {matches.Length}: {string.Join(", ", matches)}";
}

/// <summary>
///     Exception for when <see cref="float"/> and co cannot find any match.
/// </summary>
/// <param name="components">The set of components you tried to match over.</param>
public sealed class MatchNotFoundException(params Type[] components) : Exception
{
    public override string Message =>
        $"Expected precisely one entity to match the component set {string.Join(", ", components)}, but found none.";
}
