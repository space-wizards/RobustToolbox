using System;
using System.Collections.Generic;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface EntityQuery
    {
        List<Type> AllSet { get; }
        List<Type> Exclusionset { get; }
        List<Type> OneSet { get; }
    }
}
