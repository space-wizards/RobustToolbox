using System;
using System.Threading;

namespace Robust.Shared.GameObjects
{
    internal class ComponentTypeCache<T>
    {

        public static readonly int Index;
        public static readonly Type Type;
        static ComponentTypeCache()
        {
            Index = Interlocked.Increment(ref ComponentTypeCounter.TypesCount);
            Type = typeof(T);
        }

        private class ComponentTypeCounter
        {
            public static int TypesCount;
        }
    }
}
