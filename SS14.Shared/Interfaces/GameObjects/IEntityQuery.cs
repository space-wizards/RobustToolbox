using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Interfaces.GameObjects
{
    public interface IEntityQuery
    {
        IList<Type> AllSet { get; }
        IList<Type> ExclusionSet { get; }
        IList<Type> OneSet { get; }
    }
}
