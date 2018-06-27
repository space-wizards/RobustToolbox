using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.Interfaces;

namespace SS14.Server.Interfaces.Console
{
    interface IPermGroupContainer
    {
        IReadOnlyDictionary<int, IPermGroup> Groups { get; }

        void LoadGroups(IResourceManager resMan);
        void SaveGroups(IResourceManager resMan);
    }
}
