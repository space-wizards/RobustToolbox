using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Client.Graphics.Shaders
{
    internal sealed class ParsedShader
    {
        public ParsedShader(IReadOnlyDictionary<string, ShaderUniformDefinition> uniforms,
            IReadOnlyDictionary<string, ShaderVaryingDefinition> varyings, IList<ShaderFunctionDefinition> functions,
            ShaderLightMode lightMode, ShaderBlendMode blendMode, ShaderKind kind)
        {
            Uniforms = uniforms;
            Varyings = varyings;
            Functions = functions;
            LightMode = lightMode;
            BlendMode = blendMode;
            Kind = kind;
        }

        public IReadOnlyDictionary<string, ShaderUniformDefinition> Uniforms { get; }
        public IReadOnlyDictionary<string, ShaderVaryingDefinition> Varyings { get; }
        public IList<ShaderFunctionDefinition> Functions { get; }
        public ShaderLightMode LightMode { get; }
        public ShaderBlendMode BlendMode { get; }
        public ShaderKind Kind { get; }
    }

    internal sealed class ShaderFunctionDefinition
    {
        public ShaderFunctionDefinition(string name, ShaderDataType returnType,
            IReadOnlyList<ShaderFunctionParameter> parameters, string body)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
            Body = body;
        }

        public string Name { get; }
        public ShaderDataType ReturnType { get; }
        public IReadOnlyList<ShaderFunctionParameter> Parameters { get; }
        public string Body { get; }
    }

    internal sealed class ShaderFunctionParameter
    {
        public ShaderFunctionParameter(string name, ShaderDataType type, ShaderParameterQualifiers qualifiers)
        {
            Name = name;
            Type = type;
            Qualifiers = qualifiers;
        }

        public string Name { get; }
        public ShaderDataType Type { get; }
        public ShaderParameterQualifiers Qualifiers { get; }
    }

    internal sealed class ShaderVaryingDefinition
    {
        public ShaderVaryingDefinition(string name, ShaderDataType type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public ShaderDataType Type { get; }
    }

    internal sealed class ShaderUniformDefinition
    {
        public ShaderUniformDefinition(string name, ShaderDataType type, string defaultValue)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
        }

        public string Name { get; }
        public ShaderDataType Type { get; }
        public string DefaultValue { get; }
    }


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum ShaderDataType
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

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static readonly Dictionary<ShaderDataType, string> _nativeTypes = new Dictionary<ShaderDataType, string>
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
        };
    }

    internal enum ShaderKind
    {
        Sprite,
        Model
    }

    internal enum ShaderLightMode
    {
        Default = 0,
        Unshaded = 1,
    }

    internal enum ShaderBlendMode
    {
        Mix,
        Add,
        Subtract,
        Multiply
    }

    // Yeah I had no idea what to name this.
    [Flags]
    internal enum ShaderParameterQualifiers
    {
        None = 0,
        In = 1,
        Out = 2,
        Inout = 3,
    }
}
