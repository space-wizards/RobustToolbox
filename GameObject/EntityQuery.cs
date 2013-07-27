using System;
using System.Collections.Generic;

namespace GameObject
{
    public class EntityQuery
    {
        public List<Type> AllSet { get; private set; }
        public List<Type> Exclusionset { get; private set; }
        public List<Type> OneSet { get; private set; }
        public EntityQuery()
        {
            AllSet = new List<Type>();
            Exclusionset = new List<Type>();
            OneSet = new List<Type>();
        }
    }
}
