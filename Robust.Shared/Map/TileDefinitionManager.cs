using System;
using System.Collections.Generic;

namespace Robust.Shared.Map
{
    [Virtual]
    internal class TileDefinitionManager : ITileDefinitionManager
    {
        protected readonly List<ITileDefinition> TileDefs;
        private readonly Dictionary<string, ITileDefinition> _tileNames;
        private readonly Dictionary<ITileDefinition, ushort> _tileIds;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public TileDefinitionManager()
        {
            TileDefs = new List<ITileDefinition>();
            _tileNames = new Dictionary<string, ITileDefinition>();
            _tileIds = new Dictionary<ITileDefinition, ushort>();
        }

        public virtual void Initialize()
        {
        }

        public virtual void Register(ITileDefinition tileDef)
        {
            var name = tileDef.ID;
            if (_tileNames.ContainsKey(name))
            {
                throw new ArgumentException("Another tile definition with the same name has already been registered.", nameof(tileDef));
            }

            var id = checked((ushort) TileDefs.Count);
            tileDef.AssignTileId(id);
            TileDefs.Add(tileDef);
            _tileNames[name] = tileDef;
            _tileIds[tileDef] = id;
        }

        public ITileDefinition this[string name] => _tileNames[name];

        public ITileDefinition this[int id] => TileDefs[id];

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
