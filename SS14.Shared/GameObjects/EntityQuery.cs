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
            ExclusionSet = new List<Type>();
            OneSet = new List<Type>();
        }

        public IList<Type> AllSet { get; private set; }
        public IList<Type> ExclusionSet { get; private set; }
        public IList<Type> OneSet { get; private set; }
    }
}
