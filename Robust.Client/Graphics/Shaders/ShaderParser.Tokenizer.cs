using System;
using System.Collections.Generic;
using System.Text;

namespace Robust.Client.Graphics.Shaders
{
    internal partial class ShaderParser
    {
        private void _tokenize()
        {
            while (!_textParser.IsEOF())
            {
                if (_textParser.IsEOL())
                {
                    _textParser.NextLine();
                    continue;
                }

                var chr = _textParser.Peek();

                // Eat whitespace.
                if (char.IsWhiteSpace(chr))
                {
                    _textParser.Take();
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

                _textParser.Take();
                if (chr == '/')
                {
                    // Is this a comment?
                    if (_textParser.Peek() == '/')
                    {
                        // Line comment, eat until next line.
                        _eatLineComment();
                        continue;
                    }

                    else if (_textParser.Peek() == '*')
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

        private TokenSymbol _parseSymbol(char chr)
        {
            var startPos = _textParser.CurrentIndex - 1;
            var line = _textParser.CurrentLine;
            var next = _textParser.Peek();
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
                    _textParser.Take();
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
                    _textParser.Take();
                }
                else if (next == '=')
                {
                    symbol = Symbols.PlusEquals;
                    _textParser.Take();
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
                    _textParser.Take();
                }
                else if (next == '=')
                {
                    symbol = Symbols.MinusEquals;
                    _textParser.Take();
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
                    _textParser.Take();
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
                    _textParser.Take();
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
                    _textParser.Take();
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
                    _textParser.Take();
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
                    _textParser.Take();
                }
                else if (next == '=')
                {
                    symbol = Symbols.LessOrEq;
                    _textParser.Take();
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
                    _textParser.Take();
                }
                else if (next == '=')
                {
                    symbol = Symbols.GreaterOrEq;
                    _textParser.Take();
                }
                else
                {
                    symbol = Symbols.Greater;
                }
            }
            else if (chr == '^')
            {
                symbol = Symbols.Xor;
            }
            else if (chr == '&')
            {
                if (next == '&')
                {
                    symbol = Symbols.LogicAnd;
                    _textParser.Take();
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
                    _textParser.Take();
                }
                else
                {
                    symbol = Symbols.Or;
                }
            }
            else
            {
                // Unknown symbol
                throw new ShaderParseException($"Unknown symbol '{chr}'");
            }

            var endPos = _textParser.CurrentIndex;

            return new TokenSymbol(symbol, new TextFileRange(line, startPos, endPos));
        }

        private void _eatBlockComment()
        {
            var startLine = new TextFileRange(_textParser.CurrentLine, _textParser.CurrentIndex);

            // Handle nested /* nicely.
            var depth = 1;
            // Take the * of the /*.
            _textParser.Take();

            while (!_textParser.IsEOF())
            {
                if (_textParser.IsEOL())
                {
                    _textParser.NextLine();
                    continue;
                }

                var chr = _textParser.Take();
                if (chr == '/' && _textParser.Peek() == '*')
                {
                    _textParser.Take();
                    depth += 1;
                    continue;
                }

                if (chr == '*' && _textParser.Peek() == '/')
                {
                    _textParser.Take();
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
            while (!_textParser.IsEOL())
            {
                _textParser.Take();
            }
        }

        private TokenWord _parseWord()
        {
            var start = _textParser.CurrentIndex;
            var chars = new List<char>();
            while (_isWordChar(_textParser.Peek()))
            {
                chars.Add(_textParser.Take());
            }

            var end = _textParser.CurrentIndex;

            return new TokenWord(new string(chars.ToArray()), new TextFileRange(_textParser.CurrentLine, start, end));
        }

        private TokenNumber _parseDigit()
        {
            var start = _textParser.CurrentIndex;
            var end = _textParser.CurrentIndex;
            var chars = new List<char>();
            while (_isNumberChar(_textParser.Peek()))
            {
                chars.Add(_textParser.Take());
            }

            return new TokenNumber(new string(chars.ToArray()), new TextFileRange(_textParser.CurrentLine, start, end));
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

        private sealed class TokenWord : Token
        {
            public TokenWord(string word, TextFileRange position) : base(position)
            {
                Word = word;
            }

            public string Word { get; }
        }

        private sealed class TokenNumber : Token
        {
            public TokenNumber(string number, TextFileRange position) : base(position)
            {
                Number = number;
            }

            public string Number { get; }
        }

        private sealed class TokenSymbol : Token
        {
            public TokenSymbol(Symbols symbol, TextFileRange position) : base(position)
            {
                Symbol = symbol;
            }

            public Symbols Symbol { get; }
        }

        private enum Symbols
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
            Less,
            Greater,
            LessOrEq,
            GreaterOrEq,
        }

        private static readonly Dictionary<Symbols, string> _symbolStringMap = new Dictionary<Symbols, string>
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
            {Symbols.MultiplyEquals, "-="},
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
            {Symbols.LogicAnd, "&"},
            {Symbols.LogicOr, "|"},
            {Symbols.Less, "<"},
            {Symbols.LessOrEq, "<="},
            {Symbols.Greater, ">"},
            {Symbols.GreaterOrEq, ">="},
        };
    }
}
