using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Robust.Shared.GameObjects;

/// <summary>
///     An internal, typeless version of entity queries.
///     This isn't enumerable, but works for an arbitrary set of components.
/// </summary>
public readonly struct DynamicEntityQuery
{
    /// <summary>
    ///     Information on a query item, describing how to handle it.
    /// </summary>
    internal readonly struct QueryEntry
    {
        public readonly Dictionary<EntityUid, IComponent> Dict;

        public readonly QueryFlags Flags;

        public QueryEntry(Dictionary<EntityUid, IComponent> dict, QueryFlags flags)
        {
            Dict = dict;
            Flags = flags;
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
    }

    private readonly QueryEntry[] _entries;
    private readonly Dictionary<EntityUid, MetaDataComponent> _metaData;

    /// <summary>
    ///     The number of components this query is set up to emit.
    /// </summary>
    /// <remarks>
    ///     Components marked Without always get a slot in the list regardless of being used.
    /// </remarks>
    public int OutputCount => _entries.Length;

    internal DynamicEntityQuery(QueryEntry[] entries, Dictionary<EntityUid, MetaDataComponent> metaData)
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
            if ((exists ^ ((entryRef.Flags & QueryEntry.QueryFlags.Without) != 0))
                && (entryRef.Flags & QueryEntry.QueryFlags.Optional) == 0)
                return false;

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
            if ((exists ^ ((entryRef.Flags & QueryEntry.QueryFlags.Without) != 0))
                && (entryRef.Flags & QueryEntry.QueryFlags.Optional) == 0)
                return false;

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
            if ((exists ^ ((entryRef.Flags & QueryEntry.QueryFlags.Without) != 0))
                && (entryRef.Flags & QueryEntry.QueryFlags.Optional) == 0)
                return false;

            // Increment our index..
            // ReSharper disable once RedundantTypeArgumentsOfMethod
            spanEntry = ref Unsafe.Add<IComponent?>(ref spanEntry, 1);

            // and increment our index into the tails array, too.
            entryRef = ref Unsafe.Add(ref entryRef, 1);
        }

        return true; // We iterated all our tails
    }

    /// <summary>
    ///     The custom enumerator for dynamic queries.
    ///     This is intended to be an implementation detail for other queries.
    /// </summary>
    public struct Enumerator
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly Dictionary<EntityUid, IComponent> EmptyDict = new();

        private readonly DynamicEntityQuery _owner;
        private Dictionary<EntityUid, IComponent>.Enumerator _lead;

        internal Enumerator(DynamicEntityQuery owner)
        {
            QueryEntry.QueryFlags flags;
            _owner = owner;

            if (_owner._entries.Length == 0)
            {
                flags = QueryEntry.QueryFlags.None;
            }
            else
            {
                flags = _owner._entries[0].Flags;
            }

            if (flags != QueryEntry.QueryFlags.None)
            {
                throw new NotSupportedException(
                    "Query enumerators do not support optional or excluded first components.");
            }

            Reset();
        }

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

            ref var spanEntry = ref MemoryMarshal.GetReference(output);

            ent = EntityUid.Invalid;
            while (true)
            {
                if (!_lead.MoveNext())
                    return false;

                ent = _lead.Current.Key;
                spanEntry = _lead.Current.Value;

                if (spanEntry.Deleted)
                    continue; // Nevermind, move along.

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
                _lead = EmptyDict.GetEnumerator();
            }
            else
            {
                _lead = _owner._entries[0].Dict.GetEnumerator();
            }
        }
    }

    [DoesNotReturn]
    private static void ThrowBadLength(int expected, int length)
    {
        throw new IndexOutOfRangeException($"The given span is not large enough to fit all of the query's outputs. Expected {expected}, got {length}");
    }
}
