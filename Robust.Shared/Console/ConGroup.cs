using System.Collections.Generic;

namespace Robust.Shared.Console
{
    internal class ConGroup
    {
        public int Index { get; set; }

        public string Name { get; set; }

        public List<string> Commands { get; set; }

        public bool CanViewVar { get; set; }
        public bool CanAdminPlace { get; set; }
    }
}
