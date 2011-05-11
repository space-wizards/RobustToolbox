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

        public BaseTile()
        {
            AtomType = global::AtomType.Tile;
        }

        // Work out which staticgeometry array we are going to be in.
        public void SetGeoPos()
        {
            GeoPosX = (int)System.Math.Floor(Node.Position.x / 320f);
            GeoPosZ = (int)System.Math.Floor(Node.Position.z / 320f);
        }

    }
    
}
