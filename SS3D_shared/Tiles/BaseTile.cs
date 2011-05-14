using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;

namespace SS3D_shared
{
    public abstract class BaseTile : AtomBaseClass
    {
        public Material tileMaterial;
        public int GeoPosX;
        public int GeoPosZ;
        public int tileSpacing;
        public TileType TileType = TileType.None;

        public BaseTile(int _tileSpacing)
        {
            AtomType = global::AtomType.Tile;
            tileSpacing = _tileSpacing;
        }


        public BaseTile()
        {
            AtomType = global::AtomType.Tile;
        }
        // Work out which staticgeometry array we are going to be in.
        public void SetGeoPos()
        {
            GeoPosX = (int)System.Math.Floor(Node.Position.x / (tileSpacing * 10));
            GeoPosZ = (int)System.Math.Floor(Node.Position.z / (tileSpacing * 10));
        }

    }
    
}
