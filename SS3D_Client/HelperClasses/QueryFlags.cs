using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D
{
    class QueryFlags
    {   //Max = 31 Entries.
        public const uint DO_NOT_PICK = 1; //Used in the ray queries QueryMask to stop it from selecting object with this flag.
        public const uint EXAMPLE_ONE = 2;
        public const uint EXAMPLE_TWO = 4;
    };
}
