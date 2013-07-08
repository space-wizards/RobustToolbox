using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerInterfaces.GameObject
{
    public interface IEntityQuery
    {
        List<Type> AllSet { get; }
        List<Type> Exclusionset { get; }
        List<Type> OneSet { get; }
    }
}
