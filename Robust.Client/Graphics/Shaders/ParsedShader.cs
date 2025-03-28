using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Graphics
{
    internal sealed class ParsedShader
    {
        public ParsedShader(IReadOnlyDictionary<string, ShaderUniformDefinition> uniforms,
            IReadOnlyDictionary<string, ShaderVaryingDefinition> varyings,
            IReadOnlyDictionary<string, ShaderConstantDefinition> constants, IList<ShaderFunctionDefinition> functions,
            ShaderLightMode lightMode, ShaderBlendMode blendMode, ShaderPreset preset, ICollection<ResPath> includes)
        {
            Uniforms = uniforms;
            Varyings = varyings;
            Functions = functions;
            LightMode = lightMode;
            BlendMode = blendMode;
            Preset = preset;
            Includes = includes;
            Constants = constants;
        }

        [ViewVariables] public IReadOnlyDictionary<string, ShaderUniformDefinition> Uniforms { get; }
        [ViewVariables] public IReadOnlyDictionary<string, ShaderVaryingDefinition> Varyings { get; }
        [ViewVariables] public IReadOnlyDictionary<string, ShaderConstantDefinition> Constants { get; }
        [ViewVariables] public IList<ShaderFunctionDefinition> Functions { get; }
        [ViewVariables] public ShaderLightMode LightMode { get; }
        [ViewVariables] public ShaderBlendMode BlendMode { get; }
        [ViewVariables] public ShaderPreset Preset { get; }
        [ViewVariables] public ICollection<ResPath> Includes { get; }

    }

    internal sealed class ShaderFunctionDefinition
    {
        public ShaderFunctionDefinition(string name, ShaderDataTypeFull returnType,
            IReadOnlyList<ShaderFunctionParameter> parameters, string body)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
            Body = body;
        }

        public string Name { get; }
        public ShaderDataTypeFull ReturnType { get; }
        public IReadOnlyList<ShaderFunctionParameter> Parameters { get; }
        public string Body { get; }
    }

    internal sealed class ShaderFunctionParameter
    {
        public ShaderFunctionParameter(string name, ShaderDataTypeFull type, ShaderParameterQualifiers qualifiers)
        {
            Name = name;
            Type = type;
            Qualifiers = qualifiers;
        }

        public string Name { get; }
        public ShaderDataTypeFull Type { get; }
        public ShaderParameterQualifiers Qualifiers { get; }
    }

    internal sealed class ShaderVaryingDefinition
    {
        public ShaderVaryingDefinition(string name, ShaderDataTypeFull type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public ShaderDataTypeFull Type { get; }
    }

    internal sealed class ShaderUniformDefinition
    {
        public ShaderUniformDefinition(string name, ShaderDataTypeFull type, string? defaultValue)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
        }

        public string Name { get; }
        public ShaderDataTypeFull Type { get; }
        public string? DefaultValue { get; }
    }

    internal sealed class ShaderConstantDefinition
    {
        public ShaderConstantDefinition(string name, ShaderDataTypeFull type, string value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public string Name { get; }
        public ShaderDataTypeFull Type { get; }
        public string Value { get; }
    }


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum ShaderDataType : byte
    {
        Void,
        Bool,
        BVec2,
        BVec3,
        BVec4,
        Int,
        IVec2,
        IVec3,
        IVec4,
        UInt,
        UVec2,
        UVec3,
        UVec4,
        Float,
        Vec2,
        Vec3,
        Vec4,
        Mat2,
        Mat3,
        Mat4,
        Sampler2D,
        ISampler2D,
        USampler2D,
    }

    internal sealed class ShaderDataTypeFull
    {
        public ShaderDataTypeFull(ShaderDataType type, ShaderPrecisionQualifier prec, int? count = null)
        {
            Type = type;
            Precision = prec;
            Count = count;
        }

        public ShaderDataType Type { get; }
        public ShaderPrecisionQualifier Precision { get; }

        public int? Count;

        public bool IsArray => Count != null;

        public string GetNativeType()
        {
            var typeName = GetNativeTypeWithoutArray();

            return IsArray ? $"{typeName}[{Count}]" : typeName;
        }

        public string GetNativeTypeWithoutArray()
        {
            var precision = Precision switch
            {
                ShaderPrecisionQualifier.Low => "ARRAY_LOWP ",
                ShaderPrecisionQualifier.Medium => "ARRAY_MEDIUMP ",
                ShaderPrecisionQualifier.High => "ARRAY_HIGHP ",
                _ => null,
            };

            return $"{precision}{Type.GetNativeType()}";
        }

        public bool TypePrecisionConsistent()
        {
            return Type.TypeHasPrecision() == (Precision != ShaderPrecisionQualifier.None);
        }

        public bool TypeCountConsistent()
        {
            return Count == null || Type.TypeSupportsArrays();
        }
    }

    internal static class ShaderEnumExt
    {
        public static string GetNativeType(this ShaderDataType type)
        {
            return _nativeTypes[type];
        }

        public static string GetString(this ShaderParameterQualifiers qualifier)
        {
            switch (qualifier)
            {
                case ShaderParameterQualifiers.None:
                    return "";
                case ShaderParameterQualifiers.In:
                    return "in";
                case ShaderParameterQualifiers.Out:
                    return "out";
                case ShaderParameterQualifiers.Inout:
                    return "inout";
                default:
                    throw new ArgumentOutOfRangeException(nameof(qualifier), qualifier, null);
            }
        }

        public static bool TypeHasPrecision(this ShaderDataType type)
        {
            return
                (type == ShaderDataType.Float) ||
                (type == ShaderDataType.Int) ||
                (type == ShaderDataType.Vec2) ||
                (type == ShaderDataType.Vec3) ||
                (type == ShaderDataType.Vec4) ||
                (type == ShaderDataType.Mat2) ||
                (type == ShaderDataType.Mat3) ||
                (type == ShaderDataType.Mat4);
        }

        public static bool TypeSupportsArrays(this ShaderDataType type)
        {
            // TODO: add support for int, and vec3 arrays
            return
                (type == ShaderDataType.Float) ||
                (type == ShaderDataType.Vec2) ||
                (type == ShaderDataType.Bool) ||
                (type == ShaderDataType.Vec4);
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static readonly FrozenDictionary<ShaderDataType, string> _nativeTypes = new Dictionary<ShaderDataType, string>()
        {
            {ShaderDataType.Void, "void"},
            {ShaderDataType.Bool, "bool"},
            {ShaderDataType.BVec2, "bvec2"},
            {ShaderDataType.BVec3, "bvec3"},
            {ShaderDataType.BVec4, "bvec4"},
            {ShaderDataType.Int, "int"},
            {ShaderDataType.IVec2, "ivec2"},
            {ShaderDataType.IVec3, "ivec3"},
            {ShaderDataType.IVec4, "ivec4"},
            {ShaderDataType.UInt, "uint"},
            {ShaderDataType.UVec2, "uvec2"},
            {ShaderDataType.UVec3, "uvec3"},
            {ShaderDataType.UVec4, "uvec4"},
            {ShaderDataType.Float, "float"},
            {ShaderDataType.Vec2, "vec2"},
            {ShaderDataType.Vec3, "vec3"},
            {ShaderDataType.Vec4, "vec4"},
            {ShaderDataType.Mat2, "mat2"},
            {ShaderDataType.Mat3, "mat3"},
            {ShaderDataType.Mat4, "mat4"},
            {ShaderDataType.Sampler2D, "sampler2D"},
            {ShaderDataType.ISampler2D, "isampler2D"},
            {ShaderDataType.USampler2D, "usampler2D"},
        }.ToFrozenDictionary();
    }

    internal enum ShaderLightMode : byte
    {
        Default = 0,
        Unshaded = 1,
    }

    internal enum ShaderBlendMode : byte
    {
        None,
        Normal,
        Mix,
        Add,
        Subtract,
        Multiply
    }

    internal enum ShaderPreset : byte
    {
        Default,
        Raw
    }

    // Yeah I had no idea what to name this.
    [Flags]
    internal enum ShaderParameterQualifiers : byte
    {
        None = 0,
        In = 1,
        Out = 2,
        Inout = 3,
    }

    internal enum ShaderPrecisionQualifier : byte
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }
}
