using System.Collections.Generic;
using SS14.Server.Interfaces.Console;

namespace SS14.Server.Console
{
    public class PermGroup : IPermGroup
    {
        public int Index { get; set; }

        public string Name { get; set; }

        public List<string> Commands { get; set; }
    }
}
