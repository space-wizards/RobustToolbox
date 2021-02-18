using System;

namespace Robust.Shared.Utility
{
    // Based on Box2D's b2GrowableStack.

    /// <summary>
    /// This is a growable LIFO stack with an initial capacity of N.
    /// If the stack size exceeds the initial capacity, the heap is used
    /// to increase the size of the stack.
    /// </summary>
    /// <typeparam name="T">The type of elements in the stack.</typeparam>
    internal ref struct GrowableStack<T> where T : unmanaged
    {
        private Span<T> _stack;
        private int _count;
        private int _capacity;

        /// <summary>
        ///     Creates the growable stack with the allocated space as stack space.
        /// </summary>
        public GrowableStack(Span<T> stackSpace)
        {
            _stack = stackSpace;
            _capacity = stackSpace.Length;
            _count = 0;
        }

        public void Push(in T element)
        {
            if (_count == _capacity)
            {
                _capacity *= 2;
                var oldStack = _stack;
                _stack = GC.AllocateUninitializedArray<T>(_capacity);
                oldStack.CopyTo(_stack);
            }

            _stack[_count] = element;
            ++_count;
        }

        public T Pop()
        {
            --_count;
            return _stack[_count];
        }

        public int GetCount()
        {
            return _count;
        }
    }
}
