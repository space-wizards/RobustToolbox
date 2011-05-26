using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_shared.HelperClasses
{
    public class QueryFlags
    {   //Max = 31 Entries.
        public const uint DO_NOT_PICK = 1; //Used in the ray queries QueryMask to stop it from selecting object with this flag.
        public const uint ENTITY_ATOM = 2;
        public const uint ENTITY_WALL = 4;
        public const uint ENTITY_FLOOR = 8;
        public const uint ENTITY_SPACE = 16;
    };
}
