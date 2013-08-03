using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerInterfaces.GOC
{
    public interface EntityQuery
    {
        List<Type> AllSet { get; }
        List<Type> Exclusionset { get; }
        List<Type> OneSet { get; }
    }
}
