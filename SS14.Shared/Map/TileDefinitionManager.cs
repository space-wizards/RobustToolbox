using System;
using System.Collections.Generic;
using Lidgren.Network;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;

namespace SS14.Shared.Map
{
    public sealed class TileDefinitionManager : ITileDefinitionManager
    {
        //[Dependency]
        //private readonly IResourceManager resourceManager;
        private readonly List<ITileDefinition> tileDefs;
        private readonly Dictionary<string, ITileDefinition> tileNames;
        private readonly Dictionary<ITileDefinition, ushort> tileIds;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public TileDefinitionManager()
        {
            tileDefs = new List<ITileDefinition>();
            tileNames = new Dictionary<string, ITileDefinition>();
            tileIds = new Dictionary<ITileDefinition, ushort>();

            Register(new SpaceTileDefinition());
            Register(new FloorTileDefinition());
            Register(new WallTileDefinition());
        }
        
        public void InitializeResources()
        {
            /*
            foreach (var item in tileDefs)
            {
                item.InitializeResources(resourceCache);
            }
            */
        }
        
        public ushort Register(ITileDefinition tileDef)
        {
            ushort id;
            if (tileIds.TryGetValue(tileDef, out id))
                return id;

            string name = tileDef.Name;
            if (tileNames.ContainsKey(name))
                throw new ArgumentException("Another tile definition with the same name has already been registered.", nameof(tileDef));

            id = checked((ushort)tileDefs.Count);
            tileDefs.Add(tileDef);
            tileNames[name] = tileDef;
            tileIds[tileDef] = id;
            return id;
        }

        public void RegisterServerTileMapping(MsgMap message)
        {
            foreach (var tileDef in tileDefs)
                tileDef.InvalidateTileId();

            tileDefs.Clear();
            tileIds.Clear();

            for (var i = 0; i < message.TileDefs.Length; ++i)
            {
                var tileName = message.TileDefs[i].Name;
                var tileDef = this[tileName];

                tileDefs.Add(tileDef);
                tileIds[tileDef] = (ushort)i;
            }
        }

        public ITileDefinition this[string name] => tileNames[name];

        public ITileDefinition this[int id] => tileDefs[id];

        public int Count => tileDefs.Count;

        public IEnumerator<ITileDefinition> GetEnumerator()
        {
            return tileDefs.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
