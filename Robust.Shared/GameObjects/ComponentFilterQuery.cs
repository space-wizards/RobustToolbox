using System.Collections;
using System.Collections.Generic;

namespace Robust.Shared.GameObjects;

/// <summary>
///     A cacheable query for every entity that fulfills <see cref="IEntityManager.MatchesFilter"/>.
///     This provides both iteration through <see cref="IEnumerable{T}"/>, and also direct queries through <see cref="Matches"/>.
/// </summary>
/// <remarks>
///     Unlike <see cref="EntityQueryEnumerator{TComp1}"/> you can in fact just use foreach with this. It's fine!
///     A concrete implementation of <see cref="GetEnumerator"/> is provided, so the compiler knows how to optimize it.
/// </remarks>
public readonly struct ComponentFilterQuery : IEnumerable<EntityUid>
{
    private readonly Dictionary<EntityUid, IComponent> _metaData;
    private readonly Dictionary<EntityUid, IComponent> _lead;
    private readonly Dictionary<EntityUid, IComponent>[] _tails;
    private readonly bool _matchPaused;

    internal ComponentFilterQuery(Dictionary<EntityUid, IComponent> metaData, Dictionary<EntityUid, IComponent> lead, Dictionary<EntityUid, IComponent>[] tails, bool matchPaused)
    {
        _metaData = metaData;
        _lead = lead;
        _tails = tails;
        _matchPaused = matchPaused;
    }

    /// <summary>
    ///     Tests if an entity matches this query.
    /// </summary>
    /// <param name="ent">The entity to test against.</param>
    /// <returns>True on match, false otherwise.</returns>
    public bool Matches(EntityUid ent)
    {
        if (!_lead.TryGetValue(ent, out var c1) || c1.Deleted)
            return false;

        if (!_matchPaused && ((MetaDataComponent)_metaData[ent]).EntityPaused)
            return false;

        foreach (var tail in _tails)
        {
            if (!tail.TryGetValue(ent, out c1) || c1.Deleted)
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Returns an enumerator for this query.
    /// </summary>
    /// <returns></returns>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator<EntityUid> IEnumerable<EntityUid>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public struct Enumerator : IEnumerator<EntityUid>
    {
        private readonly ComponentFilterQuery _query;
        private Dictionary<EntityUid, IComponent>.Enumerator _leadEnumerator;
        private EntityUid _current;

        internal Enumerator(ComponentFilterQuery query) : this()
        {
            _query = query;
            Reset();
        }

        public bool MoveNext()
        {
            // Loop until we find something that matches every tail.
            while (true)
            {
                if (!_leadEnumerator.MoveNext())
                    return false;

                var (workingEnt, c1) = _leadEnumerator.Current;
                if (c1.Deleted)
                    continue;

                if (!_query._matchPaused && ((MetaDataComponent)_query._metaData[workingEnt]).EntityPaused)
                    continue;

                foreach (var tail in _query._tails)
                {
                    if ((!tail.TryGetValue(workingEnt, out var c2)) || c2.Deleted)
                        goto end; // Retry. We can't continue the outer loop from here easily so goto it is.
                }

                _current = workingEnt;
                break;

                end: ;
            }

            return true;
        }

        public void Reset()
        {
            _leadEnumerator = _query._lead.GetEnumerator();
        }

        EntityUid IEnumerator<EntityUid>.Current => _current;

        object IEnumerator.Current => _current;

        public void Dispose()
        {
            _leadEnumerator.Dispose();
        }
    }
}
