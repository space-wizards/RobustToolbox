using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.Interfaces.Console
{
    interface IPermGroup
    {
        int Index { get; }

        string Name { get; }

        List<string> Commands { get; }
    }
}
