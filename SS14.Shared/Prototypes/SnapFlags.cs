using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Prototypes
{
    [Flags]
    public enum SnapFlags : int
    {
        Wire = 1,
        Pipe = 2,
        Wall = 4,
        Wallmount = 8
    }
}
