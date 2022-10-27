using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Robust.Shared.Utility;

// https://github.com/dotnet/runtime/issues/934
// https://gist.github.com/LordJZ/92b7decebe52178a445a0b82f63e585a
internal static class SpanSplitExtensions
{
    public ref struct Enumerable<T> where T : IEquatable<T>
    {
        public Enumerable(ReadOnlySpan<T> span, T separator)
        {
            Span = span;
            Separator = separator;
        }

        private ReadOnlySpan<T> Span { get; }
        private T Separator { get; }

        public Enumerator<T> GetEnumerator() => new(Span, Separator);
    }

    internal ref struct Enumerator<T> where T : IEquatable<T>
    {
        internal Enumerator(ReadOnlySpan<T> span, T separator)
        {
            Span = span;
            Separator = separator;
            Current = default;

            if (Span.IsEmpty)
                TrailingEmptyItem = true;
        }

        private ReadOnlySpan<T> Span { get; set; }
        private T Separator { get; }
        private static int SeparatorLength => 1;

        private ReadOnlySpan<T> TrailingEmptyItemSentinel => Unsafe.As<T[]>(nameof(TrailingEmptyItemSentinel)).AsSpan();

        private bool TrailingEmptyItem
        {
            get => Span == TrailingEmptyItemSentinel;
            set => Span = value ? TrailingEmptyItemSentinel : default;
        }

        public bool MoveNext()
        {
            if (TrailingEmptyItem)
            {
                TrailingEmptyItem = false;
                Current = default;
                return true;
            }

            if (Span.IsEmpty)
            {
                Span = Current = default;
                return false;
            }

            var idx = Span.IndexOf(Separator);
            if (idx < 0)
            {
                Current = Span;
                Span = default;
            }
            else
            {
                Current = Span[..idx];
                Span = Span[(idx + SeparatorLength)..];
                if (Span.IsEmpty)
                    TrailingEmptyItem = true;
            }

            return true;
        }

        public ReadOnlySpan<T> Current { get; private set; }
    }

    [Pure]
    internal static Enumerable<T> Split<T>(this ReadOnlySpan<T> span, T separator) where T : IEquatable<T>
    {
        return new Enumerable<T>(span, separator);
    }
}
