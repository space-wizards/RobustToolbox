using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerInterfaces.GameObject;

namespace SGO
{
    public class EntityQuery : IEntityQuery
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
