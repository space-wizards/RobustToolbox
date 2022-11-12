using System;
using System.Collections.Generic;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables
{
    internal interface IServerViewVariablesInternal : IViewVariablesManager
    {
        void Initialize();

        ICollection<object> TraitIdsFor(Type type);
    }
}
