using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;

namespace ClientInterfaces
{
    public interface ILight
    {
        Vector2D Position { get; set; }
        int Range { get; set; }
        List<object> GetTiles();
        void ClearTiles();
        void AddTile(object tile);
        void UpdateLight();

    }
}
