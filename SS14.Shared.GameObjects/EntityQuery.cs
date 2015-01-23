using System;
using System.Collections.Generic;

namespace SS14.Shared.GameObjects
{
    public class EntityQuery
    {
        public EntityQuery()
        {
            AllSet = new List<Type>();
            Exclusionset = new List<Type>();
            OneSet = new List<Type>();
        }

        public List<Type> AllSet { get; private set; }
        public List<Type> Exclusionset { get; private set; }
        public List<Type> OneSet { get; private set; }
    }
}