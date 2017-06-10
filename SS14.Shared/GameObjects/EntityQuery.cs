using System;
using System.Collections.Generic;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Shared.GameObjects
{
    public class EntityQuery : IEntityQuery
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
