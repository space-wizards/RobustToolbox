using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Robust.Shared.GameObjects;

/// <summary>
/// <para>
///     A typeless version of entity queries optimized for more complex query behaviors.
///     This isn't enumerable for allocation reasons, but works for an arbitrary set of components.
/// </para>
/// <para>
///     DynamicEntityQuery supports <i>query constraints</i>, as described by <see cref="QueryFlags"/>,
///     that control how the query should treat the presence of a given component (is it optional, is it required).
/// </para>
/// </summary>
/// <remarks>
///     The component-returning methods on this type all take in spans to output into, it is recommended to
///     allocate an array once (within a method) and reuse it regularly.
/// </remarks>
/// <example>
/// <code>
///     var query = GetDynamicQuery(
///                     // Query every map,
///                     (typeof(MapComponent), DynamicEntityQuery.QueryFlags.None),
///                     // that may also be a grid,
///                     (typeof(MapGridComponent), DynamicEntityQuery.QueryFlags.Optional),
///                     // that is absolutely not funny.
///                     (typeof(FunnyComponent), DynamicEntityQuery.QueryFlags.Without)
///                 );
///     <br/>
///     var components = new IComponent?[3]; // Must match the number of queried components.
///     var enumerator = query.GetEnumerator();
///     <br/>
///     while (enumerator.MoveNext(out var ent, components))
///     {
///         // all the components we wanted from this ent are in components.
///         var mapComp = (MapComponent)components[0]!;
///         var mapGridComp = (MapGridComponent?)components[1];
///         // components[2] is where FunnyComponent would be, but these entities are never funny so it's always null.
///         Obliterate((ent, mapComp, mapGridComp));
///     }
/// </code>
/// </example>
/// <seealso cref="IEntityManager.GetDynamicQuery"/>
public readonly struct DynamicEntityQuery
{
    /// <summary>
    ///     Information on a query item, describing how to handle it.
    /// </summary>
    internal readonly struct QueryEntry(Dictionary<EntityUid, IComponent> dict, QueryFlags flags)
    {
        public readonly Dictionary<EntityUid, IComponent> Dict = dict;

        public readonly QueryFlags Flags = flags;
    }

    [Flags]
    public enum QueryFlags
    {
        /// <summary>
        ///     Indicates no special behavior, the component is required.
        /// </summary>
        None = 0,
        /// <summary>
        ///     Indicates this entry is optional.
        /// </summary>
        Optional = 1,
        /// <summary>
        ///     Indicates this entry is <b>excluded</b>, and the query fails if it's present.
        /// </summary>
        Without = 2,
    }

    private readonly QueryEntry[] _entries;
    private readonly Dictionary<EntityUid, IComponent> _metaData;

    /// <summary>
    ///     The number of components this query is set up to emit.
    /// </summary>
    /// <remarks>
    ///     Components marked Without always get a slot in the list regardless of being used.
    /// </remarks>
    public int OutputCount => _entries.Length;

    internal DynamicEntityQuery(QueryEntry[] entries, Dictionary<EntityUid, IComponent> metaData)
    {
        _entries = entries;
        _metaData = metaData;
    }

    /// <summary>
    ///     Tries to query an entity for this query's components.
    /// </summary>
    /// <param name="ent">The entity to look up components for.</param>
    /// <param name="output">The span to output components into.</param>
    /// <returns>True when all components were found, false otherwise.</returns>
    public bool TryGet(EntityUid ent, in Span<IComponent?> output)
    {
        // SAFETY: This ensures that the span is exactly as long as we need.
        //         Any less and we'd write out of bounds, which is Very Bad.
        if (output.Length != OutputCount)
            ThrowBadLength(OutputCount, output.Length);

        ref var spanEntry = ref MemoryMarshal.GetReference(output);

        var entriesLength = _entries.Length;

        if (entriesLength == 0)
            return true; // Okay we got everything. And by everything, I mean nothing whatsoever.

        ref var entryRef = ref MemoryMarshal.GetReference(_entries);

        // REMARK: All this work in here is to avoid a handful of bounds checks.
        //         Frankly, this isn't that critical given we're also.. indexing a dict.
        //         and as such bounds checks probably disappear into overhead.
        //         but I figure every little bit helps in a critical path like this.
        for (var i = 0; i < entriesLength; i++)
        {
            var exists = !entryRef.Dict.TryGetValue(ent, out spanEntry) || spanEntry.Deleted;
            // If it exists (or doesn't exist when without is set) and optional is not set, bail.
            if ((exists ^ ((entryRef.Flags & QueryFlags.Without) != 0))
                && (entryRef.Flags & QueryFlags.Optional) == 0)
                return false;

            if (i >= entriesLength - 1)
                break; // Don't create out of bounds refs, I like having a face.

            // Increment our index..
            // ReSharper disable once RedundantTypeArgumentsOfMethod
            spanEntry = ref Unsafe.Add<IComponent?>(ref spanEntry, 1);

            // and increment our index into the tails array, too.
            entryRef = ref Unsafe.Add(ref entryRef, 1);
        }

        return true; // We iterated all our tails
    }

    /// <summary>
    ///     Tries to query an entity for this query's components, skipping already-filled entries.
    /// </summary>
    /// <param name="ent">The entity to look up components for.</param>
    /// <param name="output">The span to output components into.</param>
    /// <returns>True when all components were found, false otherwise.</returns>
    public bool TryResolve(EntityUid ent, in Span<IComponent?> output)
    {
        // SAFETY: This ensures that the span is exactly as long as we need.
        //         Any less and we'd write out of bounds, which is Very Bad.
        if (output.Length != OutputCount)
            ThrowBadLength(OutputCount, output.Length);

        ref var spanEntry = ref MemoryMarshal.GetReference(output);

        var entriesLength = _entries.Length;

        if (entriesLength == 0)
            return true; // Okay we got everything. And by everything, I mean nothing whatsoever.

        ref var entryRef = ref MemoryMarshal.GetReference(_entries);

        // REMARK: All this work in here is to avoid a handful of bounds checks.
        //         Frankly, this isn't that critical given we're also.. indexing a dict.
        //         and as such bounds checks probably disappear into overhead.
        //         but I figure every little bit helps in a critical path like this.
        for (var i = 0; i < entriesLength; i++)
        {
            // If the entry is null and we're marked Without, continue.
            // and vice versa, if it's not null and we're marked Without, don't continue.
            // ..and if it's not null and we're not marked without, continue.
            // Resolve behavior here is a little silly, I think. Oh well.
            if (spanEntry is not null ^ ((entryRef.Flags & QueryFlags.Without) != 0))
                continue;

            var exists = !entryRef.Dict.TryGetValue(ent, out spanEntry) || spanEntry.Deleted;
            // If it exists (or doesn't exist when without is set) and optional is not set, bail.
            if ((exists ^ ((entryRef.Flags & QueryFlags.Without) != 0))
                && (entryRef.Flags & QueryFlags.Optional) == 0)
                return false;

            if (i >= entriesLength - 1)
                break; // Don't create out of bounds refs, I like having a face.

            // Increment our index..
            // ReSharper disable once RedundantTypeArgumentsOfMethod
            spanEntry = ref Unsafe.Add<IComponent?>(ref spanEntry, 1);

            // and increment our index into the tails array, too.
            entryRef = ref Unsafe.Add(ref entryRef, 1);
        }

        return true; // We iterated all our tails
    }

    /// <summary>
    ///     Tests if a given entity matches this query (ala <see cref="EntityManager.HasComponent{T}(EntityUid)"/>)
    /// </summary>
    /// <param name="ent">The entity to try matching against.</param>
    /// <returns>True if the entity matches this query.</returns>
    public bool Matches(EntityUid ent)
    {
        var entriesLength = _entries.Length;

        if (entriesLength == 0)
            return true; // Okay we got everything, as in literally nothing, but it DOES match.

        ref var entryRef = ref MemoryMarshal.GetReference(_entries);

        // REMARK: All this work in here is to avoid a handful of bounds checks.
        //         Frankly, this isn't that critical given we're also.. indexing a dict.
        //         and as such bounds checks probably disappear into overhead.
        //         but I figure every little bit helps in a critical path like this.
        for (var i = 0; i < entriesLength; i++)
        {
            var exists = !entryRef.Dict.TryGetValue(ent, out var entry) || entry.Deleted;

            // If it exists (or doesn't exist when without is set) and optional is not set, bail.
            if ((exists ^ ((entryRef.Flags & QueryFlags.Without) != 0))
                && (entryRef.Flags & QueryFlags.Optional) == 0)
                return false;

            if (i >= entriesLength - 1)
                break; // Don't create out of bounds refs, I like having a face.

            // and increment our index into the tails array, too.
            entryRef = ref Unsafe.Add(ref entryRef, 1);
        }

        return true; // We iterated all our component dicts, we win.
    }

    /// <summary>
    ///     Tries to query an entity for this query's components. Implementation detail of the enumerator, this skips
    ///     the first entry.
    /// </summary>
    /// <param name="ent">The entity to look up components for.</param>
    /// <param name="spanEntry">The span to output components into, in ref form.</param>
    /// <returns>True when all components were found, false otherwise.</returns>
    private bool TryGetAfterFirst(EntityUid ent, ref IComponent? spanEntry)
    {
        // SAFETY: We already checked the bounds if this is called.

        var entriesLength = _entries.Length;

        if (entriesLength - 1 == 0)
            return true; // Okay we got everything. And by everything, I mean nothing whatsoever.

        ref var entryRef = ref MemoryMarshal.GetReference(_entries);

        // + 1, we already got the first.
        entryRef = ref Unsafe.Add(ref entryRef, 1);

        // REMARK: All this work in here is to avoid a handful of bounds checks.
        //         Frankly, this isn't that critical given we're also.. indexing a dict.
        //         and as such bounds checks probably disappear into overhead.
        //         but I figure every little bit helps in a critical path like this.
        for (var i = 1; i < entriesLength; i++)
        {
            var exists = !entryRef.Dict.TryGetValue(ent, out spanEntry) || spanEntry.Deleted;
            // If it exists (or doesn't exist when without is set) and optional is not set, bail.
            if ((exists ^ ((entryRef.Flags & QueryFlags.Without) != 0))
                && (entryRef.Flags & QueryFlags.Optional) == 0)
                return false;

            if (i >= entriesLength - 1)
                break; // Don't create out of bounds refs, I like having a face.

            // Increment our index..
            // ReSharper disable once RedundantTypeArgumentsOfMethod
            spanEntry = ref Unsafe.Add<IComponent?>(ref spanEntry, 1);

            // and increment our index into the tails array, too.
            entryRef = ref Unsafe.Add(ref entryRef, 1);
        }

        return true; // We iterated all our tails
    }

    public Enumerator GetEnumerator(bool checkPaused)
    {
        return new Enumerator(this, checkPaused);
    }

    /// <summary>
    ///     The custom enumerator for dynamic queries.
    ///     This is intended to be an implementation detail for other queries.
    /// </summary>
    public struct Enumerator
    {
        private readonly DynamicEntityQuery _owner;
        private readonly bool _checkPaused;
        private Dictionary<EntityUid, IComponent>.Enumerator _lead;
#if DEBUG
        // Anti-misuse assertions, store enumerators for every entry and Reset() them constantly so that
        // if you update the ECS while we're enumerating it blows up.
        // This does mean DynamicEntityQuery allocates in debug.
        private readonly Dictionary<EntityUid, IComponent>.Enumerator[] _mines;
#endif

        internal Enumerator(DynamicEntityQuery owner, bool checkPaused)
        {
            QueryFlags flags;
            _owner = owner;
            _checkPaused = checkPaused;

            if (_owner._entries.Length == 0)
            {
                flags = QueryFlags.None;
            }
            else
            {
                flags = _owner._entries[0].Flags;
            }

            if (flags != QueryFlags.None)
            {
                throw new NotSupportedException(
                    "Query enumerators do not support optional or excluded first components.");
            }

#if DEBUG
            _mines = _owner._entries.Select(x => x.Dict.GetEnumerator()).ToArray();
#endif

            Reset();
        }

#if DEBUG
        private void StepOnMines()
        {
            foreach (var mine in _mines)
            {
                ((IEnumerator)mine).Reset();
            }
        }
#endif

        /// <summary>
        ///     Attempts to find the next entity in the query iterator.
        /// </summary>
        /// <param name="ent">The discovered entity, if any.</param>
        /// <param name="output">The storage for components queried for this entity.</param>
        /// <returns>True if ent and components are valid, false if there's no items left.</returns>
        public bool MoveNext(out EntityUid ent, in Span<IComponent?> output)
        {
            if (output.Length != _owner.OutputCount)
                ThrowBadLength( _owner.OutputCount, output.Length);

#if DEBUG
            StepOnMines();
#endif

            // We grab this here to pin it all function instead of constantly pinning in the loop.
            ref var span = ref MemoryMarshal.GetReference(output);
            var meta = _owner._metaData;

            ent = EntityUid.Invalid;
            while (true)
            {
                if (!_lead.MoveNext())
                    return false;

                ref var spanEntry = ref span;

                ent = _lead.Current.Key;
                spanEntry = _lead.Current.Value;

                if (_checkPaused && ((MetaDataComponent)meta[ent]).EntityPaused)
                    continue; // Oops, paused.

                if (spanEntry.Deleted)
                    continue; // Nevermind, move along.

                if (output.Length == 1)
                    return true; // Already done. Do NOT create out of bounds refs!

                // Increment our index.
                // ReSharper disable once RedundantTypeArgumentsOfMethod
                spanEntry = ref Unsafe.Add<IComponent?>(ref spanEntry, 1);

                if (_owner.TryGetAfterFirst(ent, ref spanEntry))
                    return true;

                // Oops, we failed, try again.
            }
        }

        public void Reset()
        {
            if (_owner._entries.Length == 0)
            {
                _lead = _owner._metaData.GetEnumerator();
            }
            else
            {
                _lead = _owner._entries[0].Dict.GetEnumerator();
            }

#if DEBUG
            StepOnMines();
#endif
        }
    }

    [DoesNotReturn]
    private static void ThrowBadLength(int expected, int length)
    {
        throw new IndexOutOfRangeException($"The given span is not large enough to fit all of the query's outputs. Expected {expected}, got {length}");
    }
}
