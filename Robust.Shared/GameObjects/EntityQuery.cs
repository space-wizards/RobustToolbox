using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

/// <summary>
///     An index of all entities with a given component, avoiding looking up the component's storage every time.
///     Using these saves on dictionary lookups, making your code slightly more efficient, and ties in nicely with
///     <see cref="Entity{T}"/>.
/// </summary>
/// <typeparam name="TComp1">Any component type.</typeparam>
/// <example>
///     <code>
///         public sealed class MySystem : EntitySystem
///         {
///             [Dependency] private EntityQuery&lt;TransformComponent&gt; _transforms = default!;
///             <br/>
///             public void Update(float ft)
///             {
///                 foreach (var ent in _transforms)
///                 {
///                     // iterate matching entities, excluding paused ones.
///                 }
///             }
///             <br/>
///             public void DoThings(EntityUid myEnt)
///             {
///                 var ent = _transforms.Get(myEnt);
///                 // ...
///             }
///         }
///     </code>
/// </example>
/// <remarks>
///     Queries hold references to <see cref="IEntityManager"/> internals, and are always up to date with the world.
///     They can not however perform mutation, if you need to add or remove components you must use
///     <see cref="EntitySystem"/> or <see cref="IEntityManager"/> methods.
/// </remarks>
/// <seealso cref="M:Robust.Shared.GameObjects.EntitySystem.GetEntityQuery``1">EntitySystem.GetEntityQuery()</seealso>
/// <seealso cref="M:Robust.Shared.GameObjects.EntityManager.GetEntityQuery``1">EntityManager.GetEntityQuery()</seealso>
[PublicAPI]
public readonly struct EntityQuery<TComp1> : IEnumerable<Entity<TComp1>>
    where TComp1 : IComponent
{
    private readonly EntityManager _entMan;
    private readonly Dictionary<EntityUid, IComponent> _traitDict;
    private readonly Dictionary<EntityUid, IComponent> _metaData;

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
    public EntityQuery<TComp1> All => new(this, true);

    internal EntityQuery(EntityManager entMan, Dictionary<EntityUid, IComponent> traitDict, Dictionary<EntityUid, IComponent> metaData)
    {
        _entMan = entMan;
        _traitDict = traitDict;
        _metaData = metaData;
        _enumeratePaused = false;
    }

    /// <summary>
    ///     Internal constructor used for <see cref="All"/>.
    /// </summary>
    private EntityQuery(EntityQuery<TComp1> derived, bool enumeratePaused)
    {
        _entMan = derived._entMan;
        _traitDict = derived._traitDict;
        _metaData = derived._metaData;
        _enumeratePaused = enumeratePaused;
    }

    /// <summary>
    ///     Gets <typeparamref name="TComp1"/> for an entity, throwing if it can't find it.
    /// </summary>
    /// <param name="uid">The entity to do a lookup for.</param>
    /// <returns>The located component.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the entity does not have a component of type <typeparamref name="TComp1"/>.</exception>
    /// <seealso cref="M:Robust.Shared.GameObjects.IEntityManager.GetComponent``1(Robust.Shared.GameObjects.EntityUid)">
    ///     IEntityManager.GetComponent&lt;T&gt;(EntityUid)
    /// </seealso>
    /// <seealso cref="M:Robust.Shared.GameObjects.EntitySystem.Comp``1(Robust.Shared.GameObjects.EntityUid)">
    ///     EntitySystem.Comp&lt;T&gt;(EntityUid)
    /// </seealso>
    [Pure]
    public TComp1 GetComponent(EntityUid uid)
    {
        if (_traitDict.TryGetValue(uid, out var comp) && !comp.Deleted)
            return (TComp1) comp;

        throw new KeyNotFoundException($"Entity {uid} does not have a component of type {typeof(TComp1)}");
    }

    /// <inheritdoc cref="GetComponent"/>
    [Pure]
    public Entity<TComp1> Get(EntityUid uid)
    {
        if (_traitDict.TryGetValue(uid, out var comp) && !comp.Deleted)
            return new Entity<TComp1>(uid, (TComp1) comp);

        throw new KeyNotFoundException($"Entity {uid} does not have a component of type {typeof(TComp1)}");
    }

    /// <summary>
    ///     Gets <typeparamref name="TComp1"/> for an entity, if it's present.
    /// </summary>
    /// <remarks>
    ///     If it is strictly errorenous for a component to not be present, you may want to use
    ///     <see cref="Resolve(Robust.Shared.GameObjects.EntityUid,ref TComp1?,bool)"/> instead.
    /// </remarks>
    /// <param name="uid">The entity to do a lookup for.</param>
    /// <param name="component">The located component, if any.</param>
    /// <returns>Whether the component was found.</returns>
    /// <seealso cref="M:Robust.Shared.GameObjects.IEntityManager.TryGetComponent``1(Robust.Shared.GameObjects.EntityUid,``0@)">
    ///     IEntityManager.TryGetComponent&lt;T&gt;(EntityUid, out T?)
    /// </seealso>
    /// <seealso cref="M:Robust.Shared.GameObjects.EntitySystem.TryComp``1(Robust.Shared.GameObjects.EntityUid,``0@)">
    ///     EntitySystem.TryComp&lt;T&gt;(EntityUid, out T?)
    /// </seealso>
    [Pure]
    public bool TryGetComponent([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out TComp1? component)
    {
        if (uid == null)
        {
            component = default;
            return false;
        }

        return TryGetComponent(uid.Value, out component);
    }

    /// <inheritdoc cref="TryGetComponent(Robust.Shared.GameObjects.EntityUid?,out TComp1?)"/>
    [Pure]
    public bool TryGetComponent(EntityUid uid, [NotNullWhen(true)] out TComp1? component)
    {
        if (_traitDict.TryGetValue(uid, out var comp) && !comp.Deleted)
        {
            component = (TComp1) comp;
            return true;
        }

        component = default;
        return false;
    }

    /// <inheritdoc cref="TryGetComponent(Robust.Shared.GameObjects.EntityUid?,out TComp1?)"/>
    [Pure]
    public bool TryComp(EntityUid uid, [NotNullWhen(true)] out TComp1? component)
        => TryGetComponent(uid, out component);

    /// <inheritdoc cref="TryGetComponent(Robust.Shared.GameObjects.EntityUid?,out TComp1?)"/>
    [Pure]
    public bool TryComp([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out TComp1? component)
        => TryGetComponent(uid, out component);

    /// <summary>
    ///     Tests if the given entity has <typeparamref name="TComp1"/>.
    /// </summary>
    /// <param name="uid">The entity to do a lookup for.</param>
    /// <returns>Whether the component exists for that entity.</returns>
    /// <remarks>If you immediately need to then look up that component, it's more efficient to use <see cref="TryComp(Robust.Shared.GameObjects.EntityUid,out TComp1?)"/>.</remarks>
    /// <seealso cref="M:Robust.Shared.GameObjects.IEntityManager.HasComponent``1(Robust.Shared.GameObjects.EntityUid)">
    ///     IEntityManager.HasComponent&lt;T&gt;(EntityUid)
    /// </seealso>
    /// <seealso cref="M:Robust.Shared.GameObjects.EntitySystem.HasComp``1(Robust.Shared.GameObjects.EntityUid)">
    ///     EntitySystem.HasComp&lt;T&gt;(EntityUid)
    /// </seealso>
    [Pure]
    public bool HasComp(EntityUid uid) => HasComponent(uid);

    /// <inheritdoc cref="HasComp(Robust.Shared.GameObjects.EntityUid)"/>
    [Pure]
    public bool HasComp([NotNullWhen(true)] EntityUid? uid) => HasComponent(uid);

    /// <inheritdoc cref="HasComp(Robust.Shared.GameObjects.EntityUid)"/>
    [Pure]
    public bool HasComponent(EntityUid uid)
    {
        return _traitDict.TryGetValue(uid, out var comp) && !comp.Deleted;
    }

    /// <inheritdoc cref="HasComp(Robust.Shared.GameObjects.EntityUid)"/>
    [Pure]
    public bool HasComponent([NotNullWhen(true)] EntityUid? uid)
    {
        return uid != null && HasComponent(uid.Value);
    }

    /// <include file='Docs.xml' path='entries/entry[@name="EntityQueryResolve"]/*'/>
    /// <param name="uid">The entity to do a lookup for.</param>
    /// <param name="component">The space to write the component into if found.</param>
    /// <param name="logMissing">Whether to log if the component is missing, for diagnostics.</param>
    /// <returns>Whether the component was found.</returns>
    /// <seealso cref="M:Robust.Shared.GameObjects.EntitySystem.Resolve``1(Robust.Shared.GameObjects.EntityUid,``0@,System.Boolean)">
    ///     EntitySystem.Resolve&lt;T&gt;(EntityUid, out T?)
    /// </seealso>
    public bool Resolve(EntityUid uid, [NotNullWhen(true)] ref TComp1? component, bool logMissing = true)
    {
        if (component != null)
        {
            DebugTools.AssertOwner(uid, component);
            return true;
        }

        if (_traitDict.TryGetValue(uid, out var comp) && !comp.Deleted)
        {
            component = (TComp1)comp;
            return true;
        }

        if (logMissing)
            _entMan.ResolveSawmill.Error($"Can't resolve \"{typeof(TComp1)}\" on entity {_entMan.ToPrettyString(uid)}!\n{Environment.StackTrace}");

        return false;
    }

    /// <include file='Docs.xml' path='entries/entry[@name="EntityQueryResolve"]/*'/>
    /// <param name="entity">The space to write the component into if found.</param>
    /// <param name="logMissing">Whether to log if the component is missing, for diagnostics.</param>
    /// <returns>Whether the component was found.</returns>
    /// <seealso cref="M:Robust.Shared.GameObjects.EntitySystem.Resolve``1(Robust.Shared.GameObjects.EntityUid,``0@,System.Boolean)">
    ///     EntitySystem.Resolve&lt;T&gt;(EntityUid, out T?)
    /// </seealso>
    public bool Resolve(ref Entity<TComp1?> entity, bool logMissing = true)
    {
        return Resolve(entity.Owner, ref entity.Comp, logMissing);
    }

    /// <summary>
    ///     Gets <typeparamref name="TComp1"/> for an entity if it's present, or null if it's not.
    /// </summary>
    /// <param name="uid">The entity to do the lookup on.</param>
    /// <returns>The component, if it exists.</returns>
    [Pure]
    public TComp1? CompOrNull(EntityUid uid)
    {
        if (TryGetComponent(uid, out var comp))
            return comp;

        return default;
    }

    /// <inheritdoc cref="GetComponent"/>
    [Pure]
    public TComp1 Comp(EntityUid uid)
    {
        return GetComponent(uid);
    }

    #region Internal

    /// <summary>
    /// Elides the component.Deleted check of <see cref="GetComponent"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal TComp1 GetComponentInternal(EntityUid uid)
    {
        if (_traitDict.TryGetValue(uid, out var comp))
            return (TComp1) comp;

        throw new KeyNotFoundException($"Entity {uid} does not have a component of type {typeof(TComp1)}");
    }

    /// <summary>
    /// Elides the component.Deleted check of <see cref="TryGetComponent(System.Nullable{Robust.Shared.GameObjects.EntityUid},out TComp1?)"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal bool TryGetComponentInternal([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out TComp1? component)
    {
        if (uid == null)
        {
            component = default;
            return false;
        }

        return TryGetComponentInternal(uid.Value, out component);
    }

    /// <summary>
    /// Elides the component.Deleted check of <see cref="TryGetComponent(System.Nullable{Robust.Shared.GameObjects.EntityUid},out TComp1?)"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal bool TryGetComponentInternal(EntityUid uid, [NotNullWhen(true)] out TComp1? component)
    {
        if (_traitDict.TryGetValue(uid, out var comp))
        {
            component = (TComp1) comp;
            return true;
        }

        component = default;
        return false;
    }

    /// <summary>
    /// Elides the component.Deleted check of <see cref="HasComponent(EntityUid)"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal bool HasComponentInternal(EntityUid uid)
    {
        return _traitDict.TryGetValue(uid, out var comp) && !comp.Deleted;
    }

    /// <summary>
    /// Elides the component.Deleted check of <see cref="Resolve"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal bool ResolveInternal(EntityUid uid, [NotNullWhen(true)] ref TComp1? component, bool logMissing = true)
    {
        if (component != null)
        {
            DebugTools.AssertOwner(uid, component);
            return true;
        }

        if (_traitDict.TryGetValue(uid, out var comp))
        {
            component = (TComp1)comp;
            return true;
        }

        if (logMissing)
            _entMan.ResolveSawmill.Error($"Can't resolve \"{typeof(TComp1)}\" on entity {_entMan.ToPrettyString(uid)}!\n{new StackTrace(1, true)}");

        return false;
    }
    /// <summary>
    /// Elides the component.Deleted check of <see cref="CompOrNull"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal TComp1? CompOrNullInternal(EntityUid uid)
    {
        if (TryGetComponent(uid, out var comp))
            return comp;

        return default;
    }

    #endregion

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator<Entity<TComp1>> IEnumerable<Entity<TComp1>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     The concrete enumerator for an EntityQuery, to assist the C# compiler in optimization.
    /// </summary>
    public struct Enumerator : IEnumerator<Entity<TComp1>>
    {
        private readonly EntityQuery<TComp1> _query;
        private Dictionary<EntityUid, IComponent>.Enumerator _traitDictEnumerator;

        /// <inheritdoc cref="P:Robust.Shared.GameObjects.EntityQuery`1.Enumerator.System#Collections#Generic#IEnumerator{Robust#Shared#GameObjects#Entity{TComp1}}#Current"/>
        public Entity<TComp1> Current { get; private set; }

        internal Enumerator(EntityQuery<TComp1> query)
        {
            _query = query;
            Reset();
        }

        public bool MoveNext()
        {
            // Loop until we find something that matches, or run out of entities.
            while (true)
            {
                if (!_traitDictEnumerator.MoveNext())
                    return false;

                var (workingEnt, c) = _traitDictEnumerator.Current;

                if (c.Deleted)
                    continue;

                // REMARK: You might think this would be better as two separate Enumerator implementations,
                //         but i'm not actually convinced. The memory overhead of one extra ref is small,
                //         and the branch is guaranteed to be consistent so the CPU will just skip over
                //         this check every time in the ignore-paused case.
                if (!_query._enumeratePaused && ((MetaDataComponent)_query._metaData[workingEnt]).EntityPaused)
                    continue;

                Current = new(workingEnt, (TComp1)c);
                break;
            }

            return true;
        }

        public void Reset()
        {
            _traitDictEnumerator = _query._traitDict.GetEnumerator();
        }

        Entity<TComp1> IEnumerator<Entity<TComp1>>.Current => Current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _traitDictEnumerator.Dispose();
        }
    }

    // I expect this one in particular to get used a bit more than most so.. optimize it :)
    /// <inheritdoc cref="M:System.Linq.Enumerable.ToList``1(System.Collections.Generic.IEnumerable{``0})"/>
    public List<Entity<TComp1>> ToList()
    {
        // Estimate the number of entries first.
        var list = new List<Entity<TComp1>>(_traitDict.Count);
        // Then add to it. Saving some allocs.
        list.AddRange(this);
        return list;
    }
}
