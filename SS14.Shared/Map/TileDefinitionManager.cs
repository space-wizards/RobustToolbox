using System;
using System.Collections.Generic;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Network.Messages;

namespace SS14.Shared.Map
{
    public class TileDefinitionManager : ITileDefinitionManager
    {
        //[Dependency]
        //private readonly IResourceManager resourceManager;
        private readonly List<ITileDefinition> _tileDefs;
        private readonly Dictionary<string, ITileDefinition> _tileNames;
        private readonly Dictionary<ITileDefinition, ushort> _tileIds;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public TileDefinitionManager()
        {
            _tileDefs = new List<ITileDefinition>();
            _tileNames = new Dictionary<string, ITileDefinition>();
            _tileIds = new Dictionary<ITileDefinition, ushort>();
        }

        public void Initialize()
        {
            new SpaceTileDefinition().Register(this);
            new FloorTileDefinition().Register(this);
        }

        public virtual ushort Register(ITileDefinition tileDef)
        {
            if (_tileIds.TryGetValue(tileDef, out ushort id))
            {
                throw new InvalidOperationException($"TileDefinition is already registered: {tileDef.GetType()}, id: {id}");
            }

            string name = tileDef.Name;
            if (_tileNames.ContainsKey(name))
            {
                throw new ArgumentException("Another tile definition with the same name has already been registered.", nameof(tileDef));
            }

            id = checked((ushort)_tileDefs.Count);
            _tileDefs.Add(tileDef);
            _tileNames[name] = tileDef;
            _tileIds[tileDef] = id;
            return id;
        }

        public void RegisterServerTileMapping(MsgMap message)
        {
            foreach (var tileDef in _tileDefs)
                tileDef.InvalidateTileId();

            _tileDefs.Clear();
            _tileIds.Clear();

            for (var i = 0; i < message.TileDefs.Length; ++i)
            {
                var tileName = message.TileDefs[i].Name;
                var tileDef = this[tileName];

                _tileDefs.Add(tileDef);
                _tileIds[tileDef] = (ushort)i;
            }
        }

        public ITileDefinition this[string name] => _tileNames[name];

        public ITileDefinition this[int id] => _tileDefs[id];

        public int Count => _tileDefs.Count;

        public IEnumerator<ITileDefinition> GetEnumerator()
        {
            return _tileDefs.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
