﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Robust.Shared.Map
{
    [Virtual]
    internal class TileDefinitionManager : ITileDefinitionManager
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        protected readonly List<ITileDefinition> TileDefs;
        private readonly Dictionary<string, ITileDefinition> _tileNames;
        private readonly Dictionary<string, List<string>> _awaitingAliases;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public TileDefinitionManager()
        {
            TileDefs = new List<ITileDefinition>();
            _tileNames = new Dictionary<string, ITileDefinition>();
            _awaitingAliases = new();
        }

        public virtual void Initialize()
        {
            foreach (var prototype in _prototypeManager.EnumeratePrototypes<TileAliasPrototype>())
            {
                AssignAlias(prototype.ID, prototype.Target);
            }
        }

        public virtual void Register(ITileDefinition tileDef)
        {
            var name = tileDef.ID;
            if (_tileNames.ContainsKey(name))
            {
                throw new ArgumentException("Another tile definition or alias with the same name has already been registered.", nameof(tileDef));
            }

            var id = checked((ushort) TileDefs.Count);
            tileDef.AssignTileId(id);
            TileDefs.Add(tileDef);
            _tileNames[name] = tileDef;

            AliasingHandleDeferred(name);
        }

        private void AliasingHandleDeferred(string name)
        {
            // Aliases may have been held back due to tiles not being registered yet, handle this.
            if (_awaitingAliases.ContainsKey(name))
            {
                var list = _awaitingAliases[name];
                _awaitingAliases.Remove(name);
                foreach (var alias in list)
                {
                    AssignAlias(alias, name);
                }
            }
        }


        public virtual void AssignAlias(string src, string dst)
        {
            if (_tileNames.ContainsKey(src))
            {
                throw new ArgumentException("Another tile definition or alias with the same name has already been registered.", nameof(src));
            }

            if (_tileNames.ContainsKey(dst))
            {
                // Simple enough, source to destination.
                _tileNames[src] = _tileNames[dst];
                AliasingHandleDeferred(src);
            }
            else
            {
                // Less simple - stash this alias for later so it appears when the target does.
                if (!_awaitingAliases.ContainsKey(dst))
                    _awaitingAliases[dst] = new();
                _awaitingAliases[dst].Add(src);
            }
        }

        public Tile GetVariantTile(string name, IRobustRandom random)
        {
            var tileDef = this[name];
            return GetVariantTile(tileDef, random);
        }

        public Tile GetVariantTile(string name, System.Random random)
        {
            var tileDef = this[name];
            return GetVariantTile(tileDef, random);
        }

        public Tile GetVariantTile(ITileDefinition tileDef, IRobustRandom random)
        {
            return new Tile(tileDef.TileId, variant: random.NextByte(tileDef.Variants));
        }

        public Tile GetVariantTile(ITileDefinition tileDef, System.Random random)
        {
            return new Tile(tileDef.TileId, variant: random.NextByte(tileDef.Variants));
        }

        public ITileDefinition this[string name] => _tileNames[name];

        public ITileDefinition this[int id] => TileDefs[id];

        public bool TryGetDefinition(string name, [NotNullWhen(true)] out ITileDefinition? definition)
        {
            return _tileNames.TryGetValue(name, out definition);
        }

        public bool TryGetDefinition(int id, [NotNullWhen(true)] out ITileDefinition? definition)
        {
            if (id >= TileDefs.Count)
            {
                definition = null;
                return false;
            }

            definition = TileDefs[id];
            return true;
        }

        public int Count => TileDefs.Count;

        public IEnumerator<ITileDefinition> GetEnumerator()
        {
            return TileDefs.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
