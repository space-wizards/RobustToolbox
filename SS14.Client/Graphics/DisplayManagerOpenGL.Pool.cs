using System.Collections.Generic;

namespace SS14.Client.Graphics
{
    internal partial class DisplayManagerOpenGL
    {
        // Pooling capacities here are arbitrary. Tweak them if you want.
        private readonly Pool<RenderCommandList> _poolCommandList = new Pool<RenderCommandList>(200);
        private readonly Pool<RenderCommandTexture> _poolCommandTexture = new Pool<RenderCommandTexture>(500);
        private readonly Pool<RenderCommandTransform> _poolCommandTransform = new Pool<RenderCommandTransform>(300);

        private RenderCommandList _getNewCommandList()
        {
            return _getFromPool(_poolCommandList);
        }

        private RenderCommandTexture _getNewCommandTexture()
        {
            return _getFromPool(_poolCommandTexture);
        }

        private RenderCommandTransform _getNewCommandTransform()
        {
            return _getFromPool(_poolCommandTransform);
        }

        private void _returnCommandList(RenderCommandList list)
        {
            list.Commands.Clear();
            _storeInPool(_poolCommandList, list);
        }

        private void _returnCommandTexture(RenderCommandTexture texture)
        {
            _storeInPool(_poolCommandTexture, texture);
        }

        private void _returnCommandTransform(RenderCommandTransform transform)
        {
            _storeInPool(_poolCommandTransform, transform);
        }

        private static T _getFromPool<T>(Pool<T> pool) where T : new()
        {
            if (pool.Count == 0)
            {
                return new T();
            }

            return pool.Pop();
        }

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
