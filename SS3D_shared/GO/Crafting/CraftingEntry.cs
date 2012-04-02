using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared.GO.Crafting
{
    [Serializable]
    public class CraftingEntry
    {
        public List<string> components = new List<string>();
        public int secondsToCreate = 5;
        public string result = "NULL";
    }
}
