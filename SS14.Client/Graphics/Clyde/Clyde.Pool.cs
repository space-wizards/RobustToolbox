using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SS14.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private int _poolListCreated;

        // We use pooling to store command list related objects.
        // These command lists are causing GC overhead over my dead body.

        // Pooling capacities here are arbitrary. Tweak them if you want.
        private readonly Pool<RenderCommandList> _poolCommandList = new Pool<RenderCommandList>(200);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private RenderCommandList _getNewCommandList()
        {
            return _getFromPool(_poolCommandList, ref _poolListCreated);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _returnCommandList(RenderCommandList list)
        {
            list.RenderCommands.Clear();
            _storeInPool(_poolCommandList, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T _getFromPool<T>(Pool<T> pool, ref int counter) where T : new()
        {
            if (pool.Count == 0)
            {
                counter += 1;
                return new T();
            }

            return pool.Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void _storeInPool<T>(Pool<T> pool, T item)
        {
            // If the pool is full just drop the value.
            if (pool.Count <= pool.PoolCapacity)
            {
                pool.Push(item);
            }
        }

        private class Pool<T> : Stack<T>
        {
            public int PoolCapacity { get; }

            public Pool(int poolSize) : base(poolSize)
            {
                PoolCapacity = poolSize;
            }
        }
    }
}
