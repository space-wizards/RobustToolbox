using System;
using System.Collections.Generic;

namespace ServerInterfaces.GOC
{
    public interface EntityQuery
    {
        List<Type> AllSet { get; }
        List<Type> Exclusionset { get; }
        List<Type> OneSet { get; }
    }
}