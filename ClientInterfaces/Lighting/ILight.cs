using System.Collections.Generic;
using GorgonLibrary;

namespace ClientInterfaces.Lighting
{
    public interface ILight
    {
        float Brightness { get; set; }
        Vector2D Position { get; }
        int Range { get; set; }
        List<object> GetTiles();
        void ClearTiles();
        void AddTile(object tile);
        void UpdateLight();
        void UpdatePosition(Vector2D newPosition);
    }
}
