using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics
{
    internal partial class ShaderParser
    {
        private TextParser? _currentParser;
        private string? _currentFileName;
        private readonly Stack<(TextParser? parser, string? fileName)> _parserStack = new();

        private void PushTokenize(TextReader reader, string fileName)
        {
            if (_currentParser != null)
            {
                _parserStack.Push((_currentParser, _currentFileName));
            }

            _currentParser = new TextParser(reader);
            _currentFileName = fileName;

            Tokenize();

            if (_parserStack.Count != 0)
            {
                (_currentParser, _currentFileName) = _parserStack.Pop();
            }
            else
            {
                _currentParser = null;
                _currentFileName = null;
            }
        }

        private void Tokenize()
        {
            while (!_currentParser!.IsEOF())
            {
                if (_currentParser.IsEOL())
                {
                    _currentParser.NextLine();
                    continue;
                }

                var chr = _currentParser.Peek();

                // Eat whitespace.
                if (char.IsWhiteSpace(chr))
                {
                    _currentParser.Take();
                    continue;
                }

                if (chr == '#')
                {
                    // Possible preprocessor things.
                    _currentParser.Take();
                    HandleProcessor();
                    continue;
                }

                // Eat words.
                if (_isWordBeginChar(chr))
                {
                    _tokens.Add(_parseWord());
                    continue;
                }

                // Eat numbers.
                if (_isDigit(chr))
                {
                    _tokens.Add(_parseDigit());
                    continue;
                }

                _currentParser.Take();
                if (chr == '/')
                {
                    // Is this a comment?
                    if (_currentParser.Peek() == '/')
                    {
                        // Line comment, eat until next line.
                        _eatLineComment();
                        continue;
                    }

                    else if (_currentParser.Peek() == '*')
                    {
                        // Start of a block comment.
                        _eatBlockComment();
                        continue;
                    }
                }

                // Not a comment either. It's a symbol then!
                _tokens.Add(_parseSymbol(chr));
            }
        }

        private void HandleProcessor()
        {
            _currentParser!.EatWhitespace();
            var word = _parseWord();

            if (word.Word == "include")
            {
                HandleInclude();
            }
            else if (word.Word == "ifdef")
            {
                HandleVerbatimProcessor(word);
            }
            else if (word.Word == "endif")
            {
                HandleVerbatimProcessor(word);
            }
            else if (word.Word == "ifndef")
            {
                HandleVerbatimProcessor(word);
            }
            else if (word.Word == "else")
            {
                HandleVerbatimProcessor(word);
            }
            else
            {
                throw new ShaderParseException($"Unknown preprocessor directive '{word.Word}'");
            }
        }

        private void HandleInclude()
        {
            _currentParser!.EatWhitespace();

            var quote = _currentParser.Take();

            if (quote != '"')
            {
                throw new ShaderParseException("Expected '\"' after 'include'.");
            }

            var pathParsing = new List<char>();
            while (_currentParser.Peek() != '"' && !_currentParser.IsEOL())
            {
                pathParsing.Add(_currentParser.Take());
            }

            if (_currentParser.IsEOL())
            {
                throw new ShaderParseException("Unterminated include path.");
            }

            _currentParser.Take(); // Quote.

            var pathString = new string(pathParsing.ToArray());
            var path = new ResourcePath(pathString);
            _includes.AddLast(path);
            using var stream = _resManager.ContentFileRead(path);
            using var reader = new StreamReader(stream, EncodingHelpers.UTF8);

            PushTokenize(reader, pathString);
        }

        private void HandleVerbatimProcessor(TokenWord word)
        {
            var basis = "#" + word.Word + " ";
            while (!_currentParser!.IsEOL())
            {
                basis += _currentParser.Take();
            }
            basis += '\n';
            _tokens.Add(new TokenWord(basis, word.Position));
        }

        private TokenSymbol _parseSymbol(char chr)
        {
            var startPos = _currentParser!.CurrentIndex - 1;
            var line = _currentParser.CurrentLine;
            var next = _currentParser.Peek();

            Symbols symbol;
            if (chr == ';')
            {
                symbol = Symbols.Semicolon;
            }

            else if (chr == ',')
            {
                symbol = Symbols.Comma;
            }

            else if (chr == '.')
            {
                symbol = Symbols.Period;
            }

            else if (chr == '=')
            {
                if (next == '=')
                {
                    symbol = Symbols.DoubleEquals;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.Equals;
                }
            }

            else if (chr == '(')
            {
                symbol = Symbols.ParenOpen;
            }

            else if (chr == ')')
            {
                symbol = Symbols.ParenClosed;
            }

            else if (chr == '[')
            {
                symbol = Symbols.BracketOpen;
            }

            else if (chr == ']')
            {
                symbol = Symbols.BracketClosed;
            }

            else if (chr == '{')
            {
                symbol = Symbols.BraceOpen;
            }

            else if (chr == '}')
            {
                symbol = Symbols.BraceClosed;
            }

            else if (chr == '+')
            {
                if (next == '+')
                {
                    symbol = Symbols.Increment;
                    _currentParser.Take();
                }
                else if (next == '=')
                {
                    symbol = Symbols.PlusEquals;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.Plus;
                }
            }

            else if (chr == '-')
            {
                if (next == '-')
                {
                    symbol = Symbols.Decrement;
                    _currentParser.Take();
                }
                else if (next == '=')
                {
                    symbol = Symbols.MinusEquals;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.Minus;
                }
            }

            else if (chr == '*')
            {
                if (next == '=')
                {
                    symbol = Symbols.MultiplyEquals;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.Multiply;
                }
            }

            else if (chr == '/')
            {
                if (next == '=')
                {
                    symbol = Symbols.DivideEquals;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.Divide;
                }
            }

            else if (chr == '%')
            {
                if (next == '=')
                {
                    symbol = Symbols.ModuleEquals;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.Modulo;
                }
            }

            else if (chr == '~')
            {
                symbol = Symbols.BitNot;
            }

            else if (chr == '!')
            {
                if (next == '=')
                {
                    symbol = Symbols.NotEquals;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.Not;
                }
            }

            else if (chr == '<')
            {
                if (next == '<')
                {
                    symbol = Symbols.ShiftLeft;
                    _currentParser.Take();
                }
                else if (next == '=')
                {
                    symbol = Symbols.LessOrEq;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.Less;
                }
            }

            else if (chr == '>')
            {
                if (next == '>')
                {
                    symbol = Symbols.ShiftRight;
                    _currentParser.Take();
                }
                else if (next == '=')
                {
                    symbol = Symbols.GreaterOrEq;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.Greater;
                }
            }

            else if (chr == '^')
            {
                if (next == '^')
                {
                    symbol = Symbols.LogicXor;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.Xor;
                }
            }

            else if (chr == '&')
            {
                if (next == '&')
                {
                    symbol = Symbols.LogicAnd;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.And;
                }
            }

            else if (chr == '|')
            {
                if (next == '|')
                {
                    symbol = Symbols.LogicOr;
                    _currentParser.Take();
                }
                else
                {
                    symbol = Symbols.Or;
                }
            }

            else if (chr == '?')
            {
                symbol = Symbols.QuestionMark;
            }

            else if (chr == ':')
            {
                symbol = Symbols.Colon;
            }

            else
            {
                // Unknown symbol
                throw new ShaderParseException($"Unknown symbol '{chr}'");
            }

            var endPos = _currentParser.CurrentIndex;
            return new TokenSymbol(symbol, new TextFileRange(_currentFileName!, line, startPos, endPos));
        }

        private void _eatBlockComment()
        {
            var startLine =
                new TextFileRange(_currentFileName!, _currentParser!.CurrentLine, _currentParser.CurrentIndex);

            // Handle nested /* nicely.
            var depth = 1;

            // Take the * of the /*.
            _currentParser.Take();
            while (!_currentParser.IsEOF())
            {
                if (_currentParser.IsEOL())
                {
                    _currentParser.NextLine();
                    continue;
                }

                var chr = _currentParser.Take();
                if (chr == '/' && _currentParser.Peek() == '*')
                {
                    _currentParser.Take();
                    depth += 1;
                    continue;
                }

                if (chr == '*' && _currentParser.Peek() == '/')
                {
                    _currentParser.Take();
                    depth -= 1;
                    if (depth <= 0)
                    {
                        return;
                    }
                }
            }

            // Unterminated block comment!
            throw new ShaderParseException("Block comment does not end", startLine);
        }

        private void _eatLineComment()
        {
            while (!_currentParser!.IsEOL())
            {
                _currentParser.Take();
            }
        }

        private TokenWord _parseWord()
        {
            var start = _currentParser!.CurrentIndex;

            var chars = new List<char>();
            while (_isWordChar(_currentParser.Peek()))
            {
                chars.Add(_currentParser.Take());
            }

            var end = _currentParser.CurrentIndex;
            return new TokenWord(new string(chars.ToArray()), new TextFileRange(_currentFileName!,
                _currentParser.CurrentLine, start, end));
        }

        private TokenNumber _parseDigit()
        {
            var start = _currentParser!.CurrentIndex;
            var end = _currentParser.CurrentIndex;

            var chars = new List<char>();
            while (_isNumberChar(_currentParser.Peek()))
            {
                chars.Add(_currentParser.Take());
            }

            return new TokenNumber(new string(chars.ToArray()), new TextFileRange(_currentFileName!,
                _currentParser.CurrentLine,
                start, end));
        }

        private static bool _isWordBeginChar(char chr)
        {
            return chr == '_' || chr >= 'a' && chr <= 'z' || chr >= 'A' && chr <= 'Z';
        }

        private static bool _isWordChar(char chr)
        {
            return _isWordBeginChar(chr) || _isDigit(chr);
        }

        private static bool _isDigit(char chr)
        {
            return chr >= '0' && chr <= '9';
        }

        private static bool _isNumberChar(char chr)
        {
            return _isDigit(chr) || chr == '.';
        }

        private string _tokensToString(IEnumerable<Token> tokens)
        {
            var builder = new StringBuilder();
            foreach (var token in tokens)
            {
                switch (token)
                {
                    case TokenWord word:
                        builder.AppendFormat(" {0}", word.Word);
                        break;
                    case TokenNumber number:
                        builder.AppendFormat(" {0}", number.Number);
                        break;
                    case TokenSymbol symbol:
                        builder.AppendFormat(" {0}", _symbolStringMap[symbol.Symbol]);
                        break;
                }
            }

            return builder.ToString();
        }

        private abstract class Token
        {
            protected Token(TextFileRange position)
            {
                Position = position;
            }

            public TextFileRange Position { get; }
        }

        [DebuggerDisplay("{" + nameof(Word) + "}")]
        private sealed class TokenWord : Token
        {
            public TokenWord(string word, TextFileRange position) : base(position)
            {
                Word = word;
            }

            public string Word { get; }
        }

        [DebuggerDisplay("{" + nameof(Number) + "}")]
        private sealed class TokenNumber : Token
        {
            public TokenNumber(string number, TextFileRange position) : base(position)
            {
                Number = number;
            }

            public string Number { get; }
        }

        [DebuggerDisplay("{" + nameof(Symbol) + "}")]
        private sealed class TokenSymbol : Token
        {
            public TokenSymbol(Symbols symbol, TextFileRange position) : base(position)
            {
                Symbol = symbol;
            }

            public Symbols Symbol { get; }
        }

        private enum Symbols : byte
        {
            Semicolon,
            Comma,
            Period,
            Equals,
            ParenOpen,
            ParenClosed,
            BracketOpen,
            BracketClosed,
            BraceOpen,
            BraceClosed,
            Increment,
            Plus,
            PlusEquals,
            Decrement,
            Minus,
            MinusEquals,
            Multiply,
            MultiplyEquals,
            Divide,
            DivideEquals,
            Modulo,
            ModuleEquals,
            BitNot,
            Not,
            ShiftLeft,
            ShiftRight,
            DoubleEquals,
            NotEquals,
            And,
            Xor,
            Or,
            LogicAnd,
            LogicOr,
            LogicXor,
            Less,
            Greater,
            LessOrEq,
            GreaterOrEq,
            QuestionMark,
            Colon,
        }

        private static readonly Dictionary<Symbols, string> _symbolStringMap = new()
        {
            {Symbols.Semicolon, ";\n"},
            {Symbols.Comma, ","},
            {Symbols.Period, "."},
            {Symbols.Equals, "="},
            {Symbols.ParenOpen, "("},
            {Symbols.ParenClosed, ")"},
            {Symbols.BracketOpen, "["},
            {Symbols.BracketClosed, "]"},
            {Symbols.BraceOpen, "{\n"},
            {Symbols.BraceClosed, "}\n"},
            {Symbols.Increment, "++"},
            {Symbols.Plus, "+"},
            {Symbols.PlusEquals, "+="},
            {Symbols.Decrement, "--"},
            {Symbols.Minus, "-"},
            {Symbols.MinusEquals, "-="},
            {Symbols.Multiply, "*"},
            {Symbols.MultiplyEquals, "*="},
            {Symbols.Modulo, "%"},
            {Symbols.ModuleEquals, "%="},
            {Symbols.Divide, "/"},
            {Symbols.DivideEquals, "/="},
            {Symbols.BitNot, "~"},
            {Symbols.Not, "!"},
            {Symbols.ShiftLeft, "<<"},
            {Symbols.ShiftRight, ">>"},
            {Symbols.DoubleEquals, "=="},
            {Symbols.NotEquals, "!="},
            {Symbols.And, "&"},
            {Symbols.Xor, "^"},
            {Symbols.Or, "|"},
            {Symbols.LogicAnd, "&&"},
            {Symbols.LogicOr, "||"},
            {Symbols.LogicXor, "^^"},
            {Symbols.Less, "<"},
            {Symbols.LessOrEq, "<="},
            {Symbols.Greater, ">"},
            {Symbols.GreaterOrEq, ">="},
            {Symbols.QuestionMark, "?"},
            {Symbols.Colon, ":"},
        };
    }
}
