using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

/// <typeparam name="TComp1">Any component type.</typeparam>
/// <typeparam name="TComp2">Any component type.</typeparam>
/// <typeparam name="TComp3">Any component type.</typeparam>
/// <typeparam name="TComp4">Any component type.</typeparam>
/// <include file='Docs.xml' path='entries/entry[@name="EntityQueryT"]/*'/>
[PublicAPI]
public readonly struct EntityQuery<TComp1, TComp2, TComp3, TComp4> : IEnumerable<Entity<TComp1, TComp2, TComp3, TComp4>>
    where TComp1 : IComponent
    where TComp2 : IComponent
    where TComp3 : IComponent
    where TComp4 : IComponent
{
    /// <summary>
    ///     Our dynamic query. Will always be of form [TComp1, ...]
    /// </summary>
    private readonly DynamicEntityQuery _query;
    private readonly EntityManager _entMan;

    private readonly bool _enumeratePaused;

    /// <summary>
    ///     Returns an entity query that will include paused entities when enumerated.
    /// </summary>
    /// <remarks>
    ///     You shouldn't cache this, please, there is no way to turn it back into a normal query and it's a shorthand
    ///     only meant for <c>foreach</c>.
    /// </remarks>
    /// <example>
    /// <code>
    ///     public sealed class MySystem : EntitySystem
    ///     {
    ///         [Dependency] private EntityQuery&lt;TransformComponent&gt; _transforms = default!;
    ///         <br/>
    ///         public void Update(float ft)
    ///         {
    ///             foreach (var ent in _transforms.All)
    ///             {
    ///                 // iterate matching entities, including paused ones.
    ///             }
    ///         }
    ///     }
    /// </code>
    /// </example>
    public EntityQuery<TComp1, TComp2, TComp3, TComp4> All => new(this, true);

    internal EntityQuery(DynamicEntityQuery query, EntityManager entMan)
    {
        DebugTools.AssertEqual(query.OutputCount, 4);
        _query = query;
        _entMan = entMan;
        _enumeratePaused = false;
    }

    /// <summary>
    ///     Internal constructor used for <see cref="All"/>.
    /// </summary>
    private EntityQuery(EntityQuery<TComp1, TComp2, TComp3, TComp4> derived, bool enumeratePaused)
    {
        _query = derived._query;
        _entMan = derived._entMan;
        _enumeratePaused = enumeratePaused;
    }

    /// <summary>
    ///     Tries to query an entity for this query's components.
    /// </summary>
    /// <param name="ent">The entity to look up components for.</param>
    /// <param name="comp1">The first component</param>
    /// <param name="comp2">The second component</param>
    /// <param name="comp3">The third component</param>
    /// <returns>True when all components were found, false otherwise.</returns>
    public bool TryGet(EntityUid ent, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3, [NotNullWhen(true)] out TComp4? comp4)
    {
        var buffer = new ComponentArray();

        if (_query.TryGet(ent, buffer))
        {
            comp1 = (TComp1)buffer[0]!;
            comp2 = (TComp2)buffer[1]!;
            comp3 = (TComp3)buffer[2]!;
            comp4 = (TComp4)buffer[3]!;
            return true;
        }

        comp1 = default;
        comp2 = default;
        comp3 = default;
        comp4 = default;
        return false;
    }

    /// <summary>
    ///     Tries to query an entity for this query's components.
    /// </summary>
    /// <param name="ent">The entity to look up components for.</param>
    /// <param name="resolved">The resolved entity.</param>
    /// <returns>True when all components were found, false otherwise.</returns>
    public bool TryGet(EntityUid ent, out Entity<TComp1, TComp2, TComp3, TComp4> resolved)
    {
        if (TryGet(ent, out var c1, out var c2, out var c3, out var c4))
        {
            resolved = new(ent, c1, c2, c3, c4);
            return true;
        }

        resolved = default;
        return false;
    }

    /// <summary>
    ///     Tries to query an entity for this query's components, skipping already-filled entries.
    /// </summary>
    /// <param name="ent">The entity to look up components for.</param>
    /// <param name="comp1">The first component.</param>
    /// <param name="comp2">The second component.</param>
    /// <param name="comp3">The third component.</param>
    /// <param name="logMissing">Whether to log if the resolve fails.</param>
    /// <returns>True when all components were found, false otherwise.</returns>
    public bool Resolve(EntityUid ent, [NotNullWhen(true)] ref TComp1? comp1, [NotNullWhen(true)] ref TComp2? comp2, [NotNullWhen(true)] ref TComp3? comp3, [NotNullWhen(true)] ref TComp4? comp4, bool logMissing = true)
    {
        var buffer = new ComponentArray();
        buffer[0] = comp1;
        buffer[1] = comp2;
        buffer[2] = comp3;
        buffer[3] = comp4;

        if (_query.TryResolve(ent, buffer))
        {
            comp1 = (TComp1)buffer[0]!;
            comp2 = (TComp2)buffer[1]!;
            comp3 = (TComp3)buffer[2]!;
            comp4 = (TComp4)buffer[3]!;
            return true;
        }

        if (logMissing)
            _entMan.ResolveSawmill.Error($"Can't resolve \"{typeof(TComp1)}\", \"{typeof(TComp2)}\", \"{typeof(TComp3)}\" and \"{typeof(TComp4)}\" on entity {_entMan.ToPrettyString(ent)}!\n{Environment.StackTrace}");

        return false;
    }

    /// <summary>
    ///     Tries to query an entity for this query's components, skipping already-filled entries.
    /// </summary>
    /// <param name="entity">The entity to look up components for.</param>
    /// <param name="logMissing">Whether to log if the resolve fails.</param>
    /// <returns>True when all components were found, false otherwise.</returns>
    public bool Resolve(ref Entity<TComp1?, TComp2?, TComp3?, TComp4?> entity, bool logMissing = true)
    {
        return Resolve(entity.Owner, ref entity.Comp1, ref entity.Comp2, ref entity.Comp3, ref entity.Comp4, logMissing);
    }

    /// <summary>
    ///     Tests if a given entity matches this query (ala <see cref="EntityManager.HasComponent{T}(EntityUid)"/>)
    /// </summary>
    /// <param name="ent">The entity to try matching against.</param>
    /// <returns>True if the entity matches this query.</returns>
    public bool Matches(EntityUid ent)
    {
        return _query.Matches(ent);
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator<Entity<TComp1, TComp2, TComp3, TComp4>> IEnumerable<Entity<TComp1, TComp2, TComp3, TComp4>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     Inline storage for the components we're enumerating.
    ///     Basically just working around the fact you can't stackalloc the span, error CS0208.
    /// </summary>
    [InlineArray(4)]
    private struct ComponentArray
    {
        public IComponent? Entry;
    }

    public struct Enumerator : IEnumerator<Entity<TComp1, TComp2, TComp3, TComp4>>
    {
        private DynamicEntityQuery.Enumerator _enumerator;
        public Entity<TComp1, TComp2, TComp3, TComp4> Current { get; private set; }

        internal Enumerator(EntityQuery<TComp1, TComp2, TComp3, TComp4> owner)
        {
            _enumerator = owner._query.GetEnumerator(!owner._enumeratePaused);
        }

        public bool MoveNext()
        {
            var buffer = new ComponentArray();

            if (_enumerator.MoveNext(out var ent, buffer!))
            {
                Current = new(ent, (TComp1)buffer[0]!, (TComp2)buffer[1]!, (TComp3)buffer[2]!, (TComp4)buffer[3]!);
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        Entity<TComp1, TComp2, TComp3, TComp4> IEnumerator<Entity<TComp1, TComp2, TComp3, TComp4>>.Current => Current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            // Nothin'
        }
    }
}
