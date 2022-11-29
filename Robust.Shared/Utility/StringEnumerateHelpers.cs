using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Robust.Shared.Utility;

internal static class StringEnumerateHelpers
{
    internal struct SubstringRuneEnumerator : IEnumerable<Rune>, IEnumerator<Rune>
    {
        private readonly string _source;
        private int _nextChar;
        private Rune _current;

        public SubstringRuneEnumerator(string source, int firstChar)
        {
            _source = source;
            _nextChar = firstChar;
            _current = default;
        }

        public bool MoveNext()
        {
            if (_nextChar >= _source.Length)
                return false;

            if (!Rune.TryGetRuneAt(_source, _nextChar, out _current))
                _current = Rune.ReplacementChar;

            _nextChar += _current.Utf16SequenceLength;
            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public readonly Rune Current => _current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            // Nada.
        }

        public SubstringRuneEnumerator GetEnumerator() => this;

        IEnumerator<Rune> IEnumerable<Rune>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal struct SubstringReverseRuneEnumerator : IEnumerator<Rune>, IEnumerable<Rune>
    {
        private string _source;
        // Contains the next char to return.
        // If the next char is actually a (valid) surrogate pair, this is INSIDE the pair,
        // and MoveNext() has to skip more.
        private int _nextChar;
        private Rune _current;

        public SubstringReverseRuneEnumerator(string source, int startChar)
        {
            _source = source;
            _nextChar = startChar - 1;
            _current = default;
        }

        public bool MoveNext()
        {
            if (_nextChar < 0)
                return false;

            var chr = _source[_nextChar];
            if (!char.IsSurrogate(chr))
            {
                _current = new Rune(chr);
            }
            else if (char.IsLowSurrogate(chr) && _nextChar >= 1)
            {
                var prevChr = _source[_nextChar - 1];
                if (char.IsHighSurrogate(prevChr))
                    _current = new Rune(prevChr, chr);
                else
                    _current = Rune.ReplacementChar;
            }
            else
            {
                _current = Rune.ReplacementChar;
            }

            _nextChar -= _current.Utf16SequenceLength;
            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public Rune Current => _current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            // Nada.
        }

        public SubstringReverseRuneEnumerator GetEnumerator() => this;

        IEnumerator<Rune> IEnumerable<Rune>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
