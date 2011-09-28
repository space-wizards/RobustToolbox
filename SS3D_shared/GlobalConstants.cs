using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace SS3D_shared
{
    public static class Constants
    {
        public const byte NORTH = 1;
        public const byte EAST = 2;
        public const byte SOUTH = 4;
        public const byte WEST = 8;

        public enum MoveDirs
        {
            north,
            northeast,
            east,
            southeast,
            south,
            southwest,
            west,
            northwest
        }
    }    
}
