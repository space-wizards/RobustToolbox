using SS14.Shared.Interfaces.Map;

namespace SS14.Server.Interfaces.Maps
{
    public interface IMapLoader
    {
        void Load(string filename, IMap map);
        void Save(string filename, IMap map);
    }
}
