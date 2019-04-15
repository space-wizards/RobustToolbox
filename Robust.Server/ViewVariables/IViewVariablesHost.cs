using System;
using System.Collections.Generic;

namespace Robust.Server.ViewVariables
{
    internal interface IViewVariablesHost
    {
        void Initialize();

        ICollection<object> TraitIdsFor(Type type);
    }
}
