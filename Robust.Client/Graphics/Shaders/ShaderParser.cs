using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics
{
    internal sealed partial class ShaderParser
    {
        private readonly IResourceManager _resManager;
        private int _tokenIndex;
        private readonly List<Token> _tokens = new();

        private readonly List<ShaderUniformDefinition> _uniformsParsing = new();
        private readonly List<ShaderConstantDefinition> _constantsParsing = new();
        private readonly List<ShaderVaryingDefinition> _varyingsParsing = new();
        private readonly List<ShaderFunctionDefinition> _functionsParsing = new();
        private readonly LinkedList<ResourcePath> _includes = new();

        public static ParsedShader Parse(TextReader reader, IResourceManager resManager)
        {
            var parser = new ShaderParser(resManager);
            parser.PushTokenize(reader, "<anonymous>");

            return parser._parse();
        }

        public static ParsedShader Parse(string code, IResourceManager resManager)
        {
            using var reader = new StringReader(code);

            return Parse(reader, resManager);
        }

        private ShaderParser(IResourceManager resManager)
        {
            _resManager = resManager;
        }

        private ParsedShader _parse()
        {
            ShaderLightMode? lightMode = null;
            ShaderBlendMode? blendMode = null;
            ShaderPreset? preset = null;

            Token? token;

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
                    throw new ShaderParseException(
                        "Expected 'light_mode', 'blend_mode', 'uniform', 'varying', 'preset' or type.",
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
                        throw new ShaderParseException("Expected 'unshaded'", token?.Position);
                    }

                    lightMode = ShaderLightMode.Unshaded;

                    token = _takeToken();
                    if (!(token is TokenSymbol semicolonUnshadedSymbol) ||
                        semicolonUnshadedSymbol.Symbol != Symbols.Semicolon)
                    {
                        throw new ShaderParseException("Expected ';'", token?.Position);
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

                    blendMode = token switch
                    {
                        TokenWord t when t.Word == "mix" => ShaderBlendMode.Mix,
                        TokenWord t when t.Word == "add" => ShaderBlendMode.Add,
                        TokenWord t when t.Word == "subtract" => ShaderBlendMode.Subtract,
                        TokenWord t when t.Word == "multiply" => ShaderBlendMode.Multiply,
                        TokenWord t when t.Word == "none" => ShaderBlendMode.None,
                        _ => throw new ShaderParseException("Expected 'mix', 'add', 'subtract', 'none' or 'multiply'.")
                    };

                    token = _takeToken();
                    if (!(token is TokenSymbol semicolonUnshadedSymbol) ||
                        semicolonUnshadedSymbol.Symbol != Symbols.Semicolon)
                    {
                        throw new ShaderParseException("Expected ';'", token?.Position);
                    }
                }
                else if (word.Word == "preset")
                {
                    if (preset != null)
                    {
                        throw new ShaderParseException("Already specified 'preset' before!");
                    }

                    _takeToken();

                    token = _takeToken();

                    preset = token switch
                    {
                        TokenWord t when t.Word == "default" => ShaderPreset.Default,
                        TokenWord t when t.Word == "raw" => ShaderPreset.Raw,
                        _ => throw new ShaderParseException("Expected 'default' or 'raw'.")
                    };

                    token = _takeToken();
                    if (!(token is TokenSymbol semicolonUnshadedSymbol) ||
                        semicolonUnshadedSymbol.Symbol != Symbols.Semicolon)
                    {
                        throw new ShaderParseException("Expected ';'", token?.Position);
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

                if (word.Word == "const")
                {
                    ParseConstant();
                    continue;
                }

                _parseFunction();
            }

            return new ParsedShader(
                _uniformsParsing.ToDictionary(p => p.Name, p => p),
                _varyingsParsing.ToDictionary(p => p.Name, p => p),
                _constantsParsing.ToDictionary(p => p.Name, p => p),
                _functionsParsing,
                lightMode ?? ShaderLightMode.Default,
                blendMode ?? ShaderBlendMode.Mix,
                preset ?? ShaderPreset.Default,
                _includes);
        }

        private void _parseFunction()
        {
            var retType = _parseShaderType();

            var token = _takeToken();
            if (!(token is TokenWord nameToken))
            {
                throw new ShaderParseException("Expected function name.", token?.Position);
            }

            var name = nameToken.Word;

            token = _takeToken();
            if (!(token is TokenSymbol parenthesesOpenToken) || parenthesesOpenToken.Symbol != Symbols.ParenOpen)
            {
                throw new ShaderParseException("Expected '('.", token?.Position);
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
                    if (!(token is TokenWord paramTypeOrQualifierToken))
                    {
                        throw new ShaderParseException("Expected type, 'in', 'out' or 'inout'.", token?.Position);
                    }

                    var qualifier = ShaderParameterQualifiers.None;

                    TokenWord? paramTypeToken = null;
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
                            throw new ShaderParseException("Expected type ')'.", token?.Position);
                        }

                        paramTypeToken = paramTypeTokenForReal;
                    }

                    _revToken();
                    var paramType = _parseShaderType();

                    token = _takeToken();
                    if (!(token is TokenWord paramNameToken))
                    {
                        throw new ShaderParseException("Expected parameter name.", token?.Position);
                    }

                    var paramName = paramNameToken.Word;

                    paramsParsed.Add(new ShaderFunctionParameter(paramName, paramType, qualifier));

                    token = _takeToken();
                    if (token is TokenSymbol commaOrCloseParenToken)
                    {
                        if (commaOrCloseParenToken.Symbol == Symbols.Comma)
                        {
                            token = _takeToken();
                            continue;
                        }

                        if (commaOrCloseParenToken.Symbol == Symbols.ParenClosed)
                        {
                            break;
                        }
                    }

                    throw new ShaderParseException("Expected ')' or ','", token?.Position);
                }
            }

            token = _takeToken();

            if (!(token is TokenSymbol braceOpenToken) || braceOpenToken.Symbol != Symbols.BraceOpen)
            {
                throw new ShaderParseException("Expected '{'", token?.Position);
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
            var type = _parseShaderType();

            var nameToken = _takeToken();
            if (!(nameToken is TokenWord wordName))
            {
                throw new ShaderParseException("Expected uniform name.", nameToken?.Position);
            }

            var name = wordName.Word;

            var defaultValueMaybe = _takeToken();

            if (!(defaultValueMaybe is TokenSymbol defValueMaybeSymbol))
            {
                throw new ShaderParseException("Expected ';' or '='", defaultValueMaybe?.Position);
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

        private void ParseConstant()
        {
            _takeToken();
            var type = _parseShaderType();

            var nameToken = _takeToken();
            if (!(nameToken is TokenWord wordName))
            {
                throw new ShaderParseException("Expected constant name.", nameToken?.Position);
            }

            var name = wordName.Word;

            var equals = _takeToken();

            if (!(equals is TokenSymbol equalsSymbol) || equalsSymbol.Symbol != Symbols.Equals)
            {
                throw new ShaderParseException("Expected '='", equals?.Position);
            }

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
            var def = new ShaderConstantDefinition(name, type, defValue);
            _constantsParsing.Add(def);
        }

        private void _parseVarying()
        {
            _takeToken();
            var type = _parseShaderType();

            var nameToken = _takeToken();
            if (!(nameToken is TokenWord wordName))
            {
                throw new ShaderParseException("Expected varying name.", nameToken?.Position);
            }

            var name = wordName.Word;

            var semicolonToken = _takeToken();

            if (!(semicolonToken is TokenSymbol semicolon) || semicolon.Symbol != Symbols.Semicolon)
            {
                throw new ShaderParseException("Expected ';'", semicolonToken?.Position);
            }

            var def = new ShaderVaryingDefinition(name, type);
            _varyingsParsing.Add(def);
        }

        [System.Diagnostics.Contracts.Pure]
        private Token? _peekToken()
        {
            if (_tokenIndex >= _tokens.Count)
            {
                return null;
            }

            return _tokens[_tokenIndex];
        }

        private Token? _takeToken()
        {
            if (_tokenIndex >= _tokens.Count)
            {
                return null;
            }

            return _tokens[_tokenIndex++];
        }

        private void _revToken()
        {
            if (_tokenIndex == 0)
            {
                throw new ShaderParseException("Managed to get parser to reverse off of the start");
            }
            _tokenIndex--;
        }

        private ShaderDataTypeFull _parseShaderType()
        {
            var precision = ShaderPrecisionQualifier.None;
            while (true) {
                var typeToken = _takeToken();
                if (!(typeToken is TokenWord wordType))
                {
                    throw new ShaderParseException("Expected type or precision", typeToken?.Position);
                }

                if (_shaderTypePrecisionMap.TryGetValue(wordType.Word, out var tmprc))
                {
                    precision = tmprc;
                    continue;
                }

                if (_shaderTypeMap.TryGetValue(wordType.Word, out var ret))
                {
                    var result = new ShaderDataTypeFull(ret, precision);
                    if (!result.TypePrecisionConsistent())
                    {
                        throw new ShaderParseException($"Type {ret} cannot accept precision {precision}", wordType.Position);
                    }
                    return result;
                }

                throw new ShaderParseException("Expected type or precision", wordType.Position);
            }
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static readonly Dictionary<string, ShaderPrecisionQualifier> _shaderTypePrecisionMap =
            new()
            {
                {"lowp", ShaderPrecisionQualifier.Low},
                {"mediump", ShaderPrecisionQualifier.Medium},
                {"highp", ShaderPrecisionQualifier.High}
            };

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static readonly Dictionary<string, ShaderDataType> _shaderTypeMap =
            new()
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
            public readonly string FileName;
            public readonly int LineStart;
            public readonly int LineEnd;
            public readonly int ColumnStart;
            public readonly int ColumnEnd;

            public TextFileRange(string fileName, int lineStart, int lineEnd, int columnStart, int columnEnd)
            {
                FileName = fileName;
                LineStart = lineStart;
                LineEnd = lineEnd;
                ColumnStart = columnStart;
                ColumnEnd = columnEnd;
            }

            public TextFileRange(string fileName, int line, int columnStart, int columnEnd)
            {
                FileName = fileName;
                LineStart = line;
                LineEnd = line;
                ColumnStart = columnStart;
                ColumnEnd = columnEnd;
            }

            public TextFileRange(string fileName, int line, int column)
            {
                FileName = fileName;
                LineStart = line;
                LineEnd = line;
                ColumnStart = column;
                ColumnEnd = column + 1;
            }

            public override string ToString()
            {
                return $"({FileName}:{LineStart}:{LineEnd}:{ColumnStart}:{ColumnEnd})";
            }
        }
    }

    internal sealed class ShaderParseException : Exception
    {
        public ShaderParseException(string message) : base(message)
        {
        }

        public ShaderParseException(string message, ShaderParser.TextFileRange? range) : this(range != null ? $"{range}: {message}" : $"EOL: {message}")
        {
        }
    }
}
