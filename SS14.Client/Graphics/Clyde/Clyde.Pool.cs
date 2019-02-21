using System.Collections.Generic;

namespace SS14.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private int _poolListCreated;
        private int _poolTextureCreated;
        private int _poolTransformCreated;

        // We use pooling to store command list related objects.
        // These command lists are causing GC overhead over my dead body.

        // Pooling capacities here are arbitrary. Tweak them if you want.
        private readonly Pool<RenderCommandList> _poolCommandList = new Pool<RenderCommandList>(200);
        private readonly Pool<RenderCommandTexture> _poolCommandTexture = new Pool<RenderCommandTexture>(1000);
        private readonly Pool<RenderCommandTransform> _poolCommandTransform = new Pool<RenderCommandTransform>(1000);

        private RenderCommandList _getNewCommandList()
        {
            return _getFromPool(_poolCommandList, ref _poolListCreated);
        }

        private RenderCommandTexture _getNewCommandTexture()
        {
            var item = _getFromPool(_poolCommandTexture, ref _poolTextureCreated);
            item.SubRegion = null;
            return item;
        }

        private RenderCommandTransform _getNewCommandTransform()
        {
            return _getFromPool(_poolCommandTransform, ref _poolTransformCreated);
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

        private static T _getFromPool<T>(Pool<T> pool, ref int counter) where T : new()
        {
            if (pool.Count == 0)
            {
                counter += 1;
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
