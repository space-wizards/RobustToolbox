using SS14.Server.Interfaces.Map;
using System;
using System.Collections.Generic;
using SS14.Shared.IoC;

namespace SS14.Server.Map
{
    [IoCTarget]
    public sealed class TileDefinitionManager : ITileDefinitionManager
    {
        List<ITileDefinition> tileDefs = new List<ITileDefinition>();
        Dictionary<string, ITileDefinition> tileNames = new Dictionary<string, ITileDefinition>();
        Dictionary<ITileDefinition, ushort> tileIds = new Dictionary<ITileDefinition, ushort>();

        public TileDefinitionManager()
        {
            Register(SpaceTileDefinition.Instance);
            Register(new FloorTileDefinition());
            Register(new WallTileDefinition());
        }

        public ushort Register(ITileDefinition tileDef)
        {
            ushort id;
            if (tileIds.TryGetValue(tileDef, out id))
                return id;

            string name = tileDef.Name;
            if (tileNames.ContainsKey(name))
                throw new ArgumentException("Another tile definition with the same name has already been registered.", "tileDef");

            id = checked((ushort)tileDefs.Count);
            tileDefs.Add(tileDef);
            tileNames[name] = tileDef;
            tileIds[tileDef] = id;
            return id;
        }

        public ITileDefinition this[string name]
        {
            get { return tileNames[name]; }
        }

        public ITileDefinition this[int id]
        {
            get { return tileDefs[id]; }
        }

        public int Count
        {
            get { return tileDefs.Count; }
        }

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
