using System;
using System.Runtime.InteropServices;

namespace Robust.Shared.Utility
{
    // Based on Box2D's b2GrowableStack.

    /// <summary>
    /// This is a growable LIFO stack with an initial capacity of N.
    /// If the stack size exceeds the initial capacity, the heap is used
    /// to increase the size of the stack.
    /// </summary>
    /// <remarks>
    ///     You MUST call <see cref="Dispose"/> when you are done with this stack or else you will have a memory leak.
    /// </remarks>
    /// <typeparam name="T">The type of elements in the stack.</typeparam>
    internal unsafe ref struct GrowableStack<T> where T : unmanaged
    {
        // TODO: Switch to managed arrays for the on-heap alloc when we have .NET 5.
        // Because with .NET 5 we can use the Pinned Object Heap.
        private T* _stack;
        private bool _wasReallocated;
        private int _count;
        private int _capacity;

        /// <summary>
        ///     Creates the growable stack with the allocated space as stack space.
        /// </summary>
        /// <remarks>
        ///     <paramref name="stackSpace"/> MUST BE A PIECE OF PINNED MEMORY,
        ///     OR ELSE YOU HAVE A MASSIVE GC BUG ON YOUR HAND.
        /// </remarks>
        public GrowableStack(Span<T> stackSpace)
        {
            fixed (T* ap = stackSpace)
            {
                _stack = ap;
            }

            _capacity = stackSpace.Length;
            _wasReallocated = false;
            _count = 0;
        }

        public void Dispose()
        {
            if (_wasReallocated)
            {
                Marshal.FreeHGlobal((IntPtr) _stack);
                _stack = null;
            }
        }

        public void Push(in T element)
        {
            if (_count == _capacity)
            {
                var old = _stack;
                _capacity *= 2;
                var dstSize = _capacity * sizeof(T);
                _stack = (T*) Marshal.AllocHGlobal(dstSize);
                Buffer.MemoryCopy(old, _stack, dstSize, _count * sizeof(T));
                if (_wasReallocated)
                {
                    Marshal.FreeHGlobal((IntPtr) old);
                }

                _wasReallocated = true;
            }

            _stack[_count] = element;
            ++_count;
        }

        public T Pop()
        {
            DebugTools.Assert(_count > 0);
            --_count;
            return _stack[_count];
        }

        public int GetCount()
        {
            return _count;
        }
    }
}
