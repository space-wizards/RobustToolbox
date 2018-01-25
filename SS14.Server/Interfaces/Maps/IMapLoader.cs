using System;
using SS14.Shared.Interfaces.Map;

namespace SS14.Server.Interfaces.Maps
{
    public interface IMapLoader
    {
        void LoadGrid(IMap map, string path);
        void LoadEntities(IMap map, string path);

        [Obsolete]
        void EntityLoader(IMap map, string filename);

        void Save(IMap map, string filename);
    }
}
