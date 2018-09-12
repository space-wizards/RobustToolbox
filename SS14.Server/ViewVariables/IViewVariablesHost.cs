using System;
using System.Collections.Generic;

namespace SS14.Server.ViewVariables
{
    internal interface IViewVariablesHost
    {
        void Initialize();

        ICollection<object> TraitIdsFor(Type type);
    }
}
