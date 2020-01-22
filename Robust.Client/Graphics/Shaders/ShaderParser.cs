using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Shaders
{
    internal sealed partial class ShaderParser
    {
        private readonly TextParser _textParser;
        private int _tokenIndex;
        private readonly List<Token> _tokens = new List<Token>();

        private readonly List<ShaderUniformDefinition> _uniformsParsing = new List<ShaderUniformDefinition>();
        private readonly List<ShaderVaryingDefinition> _varyingsParsing = new List<ShaderVaryingDefinition>();
        private readonly List<ShaderFunctionDefinition> _functionsParsing = new List<ShaderFunctionDefinition>();

        public static ParsedShader Parse(TextReader reader)
        {
            return new ShaderParser(reader)._parse();
        }

        public static ParsedShader Parse(string code)
        {
            using (var reader = new StringReader(code))
            {
                return Parse(reader);
            }
        }

        private ShaderParser(TextReader reader)
        {
            _textParser = new TextParser(reader);
        }

        private ParsedShader _parse()
        {
            _tokenize();

            ShaderLightMode? lightMode = null;
            ShaderBlendMode? blendMode = null;
            ShaderKind? shaderKind = null;

            Token token;

            while (_tokenIndex < _tokens.Count)
            {
                token = _peekToken();
                if (token == null)
                {
                    // EOF.
                    break;
                }

                if (!(token is TokenWord word))
                {
                    throw new ShaderParseException("Expected 'light_mode', 'blend_mode', 'kind', 'uniform', 'varying' or type.",
                        token.Position);
                }

                if (word.Word == "light_mode")
                {
                    if (lightMode != null)
                    {
                        throw new ShaderParseException("Already specified 'light_mode' before!");
                    }
                    _takeToken();
                    token = _takeToken();
                    if (!(token is TokenWord unshadedWord) || unshadedWord.Word != "unshaded")
                    {
                        throw new ShaderParseException("Expected 'unshaded'", token.Position);
                    }

                    lightMode = ShaderLightMode.Unshaded;

                    token = _takeToken();
                    if (!(token is TokenSymbol semicolonUnshadedSymbol) ||
                        semicolonUnshadedSymbol.Symbol != Symbols.Semicolon)
                    {
                        throw new ShaderParseException("Expected ';'", token.Position);
                    }
                }
                else if (word.Word == "blend_mode")
                {
                    if (lightMode != null)
                    {
                        throw new ShaderParseException("Already specified 'blend_mode' before!");
                    }
                    _takeToken();
                    token = _takeToken();

                    switch (token)
                    {
                        case TokenWord t when t.Word == "mix":
                            blendMode = ShaderBlendMode.Mix;
                            break;
                        case TokenWord t when t.Word == "add":
                            blendMode = ShaderBlendMode.Add;
                            break;
                        case TokenWord t when t.Word == "subtract":
                            blendMode = ShaderBlendMode.Subtract;
                            break;
                        case TokenWord t when t.Word == "multiply":
                            blendMode = ShaderBlendMode.Multiply;
                            break;
                        default:
                            throw new ShaderParseException("Expected 'mix', 'add', 'subtract' or 'multiply'.");
                    }
                }
                else if (word.Word == "kind")
                {
                    if (shaderKind != null)
                    {
                        throw new ShaderParseException("Already specified 'kind' before!");
                    }
                    _takeToken();
                    token = _takeToken();

                    switch (token)
                    {
                        case TokenWord t when t.Word == "sprite":
                            shaderKind = ShaderKind.Sprite;
                            break;
                        case TokenWord t when t.Word == "model":
                            shaderKind = ShaderKind.Model;
                            break;
                        default:
                            throw new ShaderParseException("Expected 'sprite' or 'model'.");
                    }

                    token = _takeToken();
                    if (!(token is TokenSymbol semicolonUnshadedSymbol) ||
                        semicolonUnshadedSymbol.Symbol != Symbols.Semicolon)
                    {
                        throw new ShaderParseException("Expected ';'", token.Position);
                    }
                }
                else
                {
                    break;
                }
            }

            // Main loop for parsing of structures in the root of the shader file.
            while (_tokenIndex < _tokens.Count)
            {
                token = _peekToken();
                if (token == null)
                {
                    // EOF.
                    break;
                }

                if (!(token is TokenWord word))
                {
                    throw new ShaderParseException("Expected 'uniform', 'varying' or type.",
                        token.Position);
                }

                if (word.Word == "uniform")
                {
                    _parseUniform();
                    continue;
                }

                if (word.Word == "varying")
                {
                    _parseVarying();
                    continue;
                }

                _parseFunction();
            }

            return new ParsedShader(
                _uniformsParsing.ToDictionary(p => p.Name, p => p),
                _varyingsParsing.ToDictionary(p => p.Name, p => p),
                _functionsParsing, lightMode ?? ShaderLightMode.Default, blendMode ?? ShaderBlendMode.Mix, shaderKind ?? ShaderKind.Sprite);
        }

        private void _parseFunction()
        {
            var token = _takeToken();
            if (!(token is TokenWord typeToken))
            {
                throw new ShaderParseException("Expected type.", token.Position);
            }

            var retType = _parseShaderType(typeToken);

            token = _takeToken();
            if (!(token is TokenWord nameToken))
            {
                throw new ShaderParseException("Expected function name.", token.Position);
            }

            var name = nameToken.Word;

            token = _takeToken();
            if (!(token is TokenSymbol parenthesesOpenToken) || parenthesesOpenToken.Symbol != Symbols.ParenOpen)
            {
                throw new ShaderParseException("Expected '('.", token.Position);
            }

            var paramsParsed = new List<ShaderFunctionParameter>();
            token = _takeToken();
            if (token is TokenSymbol endParenTokenMaybe)
            {
                if (endParenTokenMaybe.Symbol != Symbols.ParenClosed)
                {
                    throw new ShaderParseException("Expected ')' or type.", token.Position);
                }
            }
            else
            {
                // Parse parameters.
                while (true)
                {
                    token = _takeToken();
                    if (!(token is TokenWord paramTypeOrQualifierToken))
                    {
                        throw new ShaderParseException("Expected type, 'in', 'out' or 'inout'.", token.Position);
                    }

                    var qualifier = ShaderParameterQualifiers.None;

                    TokenWord paramTypeToken = null;
                    if (paramTypeOrQualifierToken.Word == "in")
                    {
                        qualifier = ShaderParameterQualifiers.In;
                    }
                    else if (paramTypeOrQualifierToken.Word == "out")
                    {
                        qualifier = ShaderParameterQualifiers.Out;
                    }
                    else if (paramTypeOrQualifierToken.Word == "inout")
                    {
                        qualifier = ShaderParameterQualifiers.Inout;
                    }
                    else
                    {
                        paramTypeToken = paramTypeOrQualifierToken;
                    }

                    if (paramTypeToken == null)
                    {
                        token = _takeToken();
                        if (!(token is TokenWord paramTypeTokenForReal))
                        {
                            throw new ShaderParseException("Expected type ')'.", token.Position);
                        }

                        paramTypeToken = paramTypeTokenForReal;
                    }

                    var paramType = _parseShaderType(paramTypeToken);

                    token = _takeToken();
                    if (!(token is TokenWord paramNameToken))
                    {
                        throw new ShaderParseException("Expected parameter name.", token.Position);
                    }

                    var paramName = paramNameToken.Word;

                    paramsParsed.Add(new ShaderFunctionParameter(paramName, paramType, qualifier));

                    token = _takeToken();
                    if (token is TokenSymbol commaOrCloseParenToken)
                    {
                        if (commaOrCloseParenToken.Symbol == Symbols.Comma)
                        {
                            continue;
                        }

                        if (commaOrCloseParenToken.Symbol == Symbols.ParenClosed)
                        {
                            break;
                        }
                    }

                    throw new ShaderParseException("Expected ')' or ','", token.Position);
                }
            }

            token = _takeToken();

            if (!(token is TokenSymbol braceOpenToken) || braceOpenToken.Symbol != Symbols.BraceOpen)
            {
                throw new ShaderParseException("Expected '{'", token.Position);
            }

            var tokens = new List<Token>(10);
            var braceDepth = 1;
            while (true)
            {
                // Take entire function body. We assume it's valid.
                token = _takeToken();
                if (token == null)
                {
                    throw new ShaderParseException("Hit EOF while parsing function body");
                }

                if (token is TokenSymbol braceToken)
                {
                    if (braceToken.Symbol == Symbols.BraceOpen)
                    {
                        braceDepth += 1;
                    }

                    if (braceToken.Symbol == Symbols.BraceClosed)
                    {
                        braceDepth -= 1;
                        if (braceDepth == 0)
                        {
                            break;
                        }
                    }
                }

                tokens.Add(token);
            }

            var body = _tokensToString(tokens);
            _functionsParsing.Add(new ShaderFunctionDefinition(name, retType, paramsParsed, body));
        }

        private void _parseUniform()
        {
            _takeToken();
            var typeToken = _takeToken();
            if (!(typeToken is TokenWord wordType))
            {
                throw new ShaderParseException("Expected type.", typeToken.Position);
            }

            var type = _parseShaderType(wordType);

            var nameToken = _takeToken();
            if (!(nameToken is TokenWord wordName))
            {
                throw new ShaderParseException("Expected uniform name.", nameToken.Position);
            }

            var name = wordName.Word;

            var defaultValueMaybe = _takeToken();

            if (!(defaultValueMaybe is TokenSymbol defValueMaybeSymbol))
            {
                throw new ShaderParseException("Expected ';' or '='", defaultValueMaybe.Position);
            }

            if (defValueMaybeSymbol.Symbol == Symbols.Semicolon)
            {
                // Just a semicolon, end the uniform def here.
                var def = new ShaderUniformDefinition(name, type, null);
                _uniformsParsing.Add(def);
                return;
            }

            if (defValueMaybeSymbol.Symbol == Symbols.Equals)
            {
                var tokens = new List<Token>();
                while (true)
                {
                    var token = _takeToken();
                    if (token == null)
                    {
                        throw new ShaderParseException("Got EOF while parsing uniform default value.");
                    }

                    if (token is TokenSymbol tokenSymbol && tokenSymbol.Symbol == Symbols.Semicolon)
                    {
                        break;
                    }

                    tokens.Add(token);
                }

                var defValue = _tokensToString(tokens);
                var def = new ShaderUniformDefinition(name, type, defValue);
                _uniformsParsing.Add(def);
                return;
            }

            throw new ShaderParseException("Expected ';' or '='", defaultValueMaybe.Position);
        }

        private void _parseVarying()
        {
            _takeToken();
            var typeToken = _takeToken();
            if (!(typeToken is TokenWord wordType))
            {
                throw new ShaderParseException("Expected type.", typeToken.Position);
            }

            var type = _parseShaderType(wordType);

            var nameToken = _takeToken();
            if (!(nameToken is TokenWord wordName))
            {
                throw new ShaderParseException("Expected varying name.", nameToken.Position);
            }

            var name = wordName.Word;

            var semicolonToken = _takeToken();

            if (!(semicolonToken is TokenSymbol semicolon) || semicolon.Symbol != Symbols.Semicolon)
            {
                throw new ShaderParseException("Expected ';'", semicolonToken.Position);
            }

            var def = new ShaderVaryingDefinition(name, type);
            _varyingsParsing.Add(def);
        }

        [System.Diagnostics.Contracts.Pure]
        private Token _peekToken()
        {
            if (_tokenIndex >= _tokens.Count)
            {
                return null;
            }

            return _tokens[_tokenIndex];
        }

        private Token _takeToken()
        {
            if (_tokenIndex >= _tokens.Count)
            {
                return null;
            }

            return _tokens[_tokenIndex++];
        }

        private static ShaderDataType _parseShaderType(TokenWord word)
        {
            if (_shaderTypeMap.TryGetValue(word.Word, out var ret))
            {
                return ret;
            }

            throw new ShaderParseException("Expected <type>", word.Position);
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static readonly Dictionary<string, ShaderDataType> _shaderTypeMap =
            new Dictionary<string, ShaderDataType>
            {
                {"void", ShaderDataType.Void},
                {"bool", ShaderDataType.Bool},
                {"bvec2", ShaderDataType.BVec2},
                {"bvec3", ShaderDataType.BVec3},
                {"bvec4", ShaderDataType.BVec4},
                {"int", ShaderDataType.Int},
                {"ivec2", ShaderDataType.IVec2},
                {"ivec3", ShaderDataType.IVec3},
                {"ivec4", ShaderDataType.IVec4},
                {"uint", ShaderDataType.UInt},
                {"uvec2", ShaderDataType.UVec2},
                {"uvec3", ShaderDataType.UVec3},
                {"uvec4", ShaderDataType.UVec4},
                {"float", ShaderDataType.Float},
                {"vec2", ShaderDataType.Vec2},
                {"vec3", ShaderDataType.Vec3},
                {"vec4", ShaderDataType.Vec4},
                {"mat2", ShaderDataType.Mat2},
                {"mat3", ShaderDataType.Mat3},
                {"mat4", ShaderDataType.Mat4},
                {"sampler2D", ShaderDataType.Sampler2D},
                {"isampler2D", ShaderDataType.ISampler2D},
                {"usampler2D", ShaderDataType.USampler2D},
            };

        [PublicAPI]
        internal readonly struct TextFileRange
        {
            public readonly int LineStart;
            public readonly int LineEnd;
            public readonly int ColumnStart;
            public readonly int ColumnEnd;

            public TextFileRange(int lineStart, int lineEnd, int columnStart, int columnEnd)
            {
                LineStart = lineStart;
                LineEnd = lineEnd;
                ColumnStart = columnStart;
                ColumnEnd = columnEnd;
            }

            public TextFileRange(int line, int columnStart, int columnEnd)
            {
                LineStart = line;
                LineEnd = line;
                ColumnStart = columnStart;
                ColumnEnd = columnEnd;
            }

            public TextFileRange(int line, int column) : this()
            {
                LineStart = line;
                LineEnd = line;
                ColumnStart = column;
                ColumnEnd = column + 1;
            }

            public override string ToString()
            {
                return $"({LineStart}:{LineEnd}:{ColumnStart}:{ColumnEnd})";
            }
        }
    }

    internal sealed class ShaderParseException : Exception
    {
        public ShaderParseException(string message) : base(message)
        {
        }

        public ShaderParseException(string message, ShaderParser.TextFileRange range) : this($"{range}: {message}")
        {
        }
    }
}
