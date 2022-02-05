using System;
using System.IO;

namespace Robust.Shared.Utility
{
    /// <summary>
    ///     Helper class for parsing text.
    /// </summary>
    internal sealed class TextParser
    {
        private readonly TextReader _reader;

        public int CurrentLine { get; private set; }

        /// <summary>
        ///     Index of the next character to be read.
        /// </summary>
        public int CurrentIndex { get; private set; }

        private string? _currentLine;

        public TextParser(TextReader reader)
        {
            _reader = reader;
        }

        public void NextLine()
        {
            _currentLine = _reader.ReadLine();
            CurrentIndex = 0;
            CurrentLine++;
        }

        public bool TryParse(string str)
        {
            if (IsEOL())
            {
                return false;
            }

            var valid = _currentLine!.IndexOf(str, CurrentIndex, StringComparison.Ordinal) == CurrentIndex;

            if (valid)
            {
                Advance(str.Length);
            }

            return valid;
        }

        public bool TryParse(char chr)
        {
            if (IsEOL())
            {
                return false;
            }

            var valid = _currentLine![CurrentIndex] == chr;
            if (valid)
            {
                Advance();
            }

            return valid;
        }

        public void Parse(char chr)
        {
            if (IsEOL())
            {
                throw new ParserException($"Expected '{chr}', got EOL");
            }

            if (_currentLine![CurrentIndex] != chr)
            {
                throw new ParserException($"Expected '{chr}'.");
            }

            Advance();
        }

        [System.Diagnostics.Contracts.Pure]
        public bool IsEOL()
        {
            return _currentLine == null || _currentLine.Length <= CurrentIndex;
        }

        [System.Diagnostics.Contracts.Pure]
        public bool IsEOF()
        {
            return _reader.Peek() == -1 && IsEOL();
        }

        public void EnsureEOL()
        {
            if (!IsEOL())
            {
                throw new ParserException("Expected EOL");
            }
        }

        public void EnsureNoEOL()
        {
            if (IsEOL())
            {
                throw new ParserException("Unexpected EOL");
            }
        }

        public bool EatWhitespace()
        {
            var ateAny = false;
            while (!IsEOL())
            {
                if (!char.IsWhiteSpace(_currentLine!, CurrentIndex))
                {
                    break;
                }

                Advance();
                ateAny = true;
            }

            return ateAny;
        }

        public string EatUntilWhitespace()
        {
            var current = CurrentIndex;
            while (!IsEOL())
            {
                if (char.IsWhiteSpace(_currentLine!, CurrentIndex))
                {
                    break;
                }

                Advance();
            }

            return _currentLine!.Substring(current, CurrentIndex - current);
        }

        public string EatUntilEOL()
        {
            var current = CurrentIndex;
            while (!IsEOL())
            {
                Advance();
            }

            return _currentLine!.Substring(current, CurrentIndex - current);
        }

        [System.Diagnostics.Contracts.Pure]
        public char Peek()
        {
            if (CurrentIndex >= _currentLine!.Length)
            {
                return '\0';
            }
            return _currentLine[CurrentIndex];
        }

        public char Take()
        {
            return _currentLine![CurrentIndex++];
        }

        public void Advance(int amount = 1)
        {
            CurrentIndex += amount;
        }

        // Wrapping for the various IsXXX methods on Char because Char is dumb.
        [System.Diagnostics.Contracts.Pure]
        public bool PeekIsDigit()
        {
            return char.IsDigit(_currentLine!, CurrentIndex);
        }

        [Virtual]
        public class ParserException : Exception
        {
            public ParserException(string message) : base(message)
            {
            }

            public ParserException(string message, Exception inner) : base(message, inner)
            {
            }
        }
    }
}
