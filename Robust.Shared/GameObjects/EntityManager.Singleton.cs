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

    public Entity<TComp1> SingleOrSpawn<TComp1>(EntProtoId fallback, MapCoordinates location)
        where TComp1: IComponent
    {
        if (TrySingle<TComp1>(out var ent))
            return ent.Value;

        Spawn(fallback, location);

        return Single<TComp1>();
    }

    public Entity<TComp1, TComp2> SingleOrSpawn<TComp1, TComp2>(EntProtoId fallback, MapCoordinates location)
        where TComp1: IComponent
        where TComp2: IComponent
    {
        if (TrySingle<TComp1, TComp2>(out var ent))
            return ent.Value;

        Spawn(fallback, location);

        return Single<TComp1, TComp2>();
    }

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

    public Entity<TComp1> SingleOrInit<TComp1>(Action fallback)
        where TComp1: IComponent
    {
        if (TrySingle<TComp1>(out var ent))
            return ent.Value;

        fallback();

        return Single<TComp1>();
    }

    public Entity<TComp1, TComp2> SingleOrInit<TComp1, TComp2>(Action fallback)
        where TComp1: IComponent
        where TComp2: IComponent
    {
        if (TrySingle<TComp1, TComp2>(out var ent))
            return ent.Value;

        fallback();

        return Single<TComp1, TComp2>();
    }

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
