using System;
using System.Collections.Generic;

namespace SS14.Shared.GO.Crafting
{
    [Serializable]
    public class CraftingEntry
    {
        public List<string> components = new List<string>();
        public string result = "NULL";
        public int secondsToCreate = 5;
    }
}