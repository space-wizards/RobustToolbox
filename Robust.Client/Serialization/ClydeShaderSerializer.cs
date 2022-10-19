using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.Serialization;

public sealed class ClydeShaderSerializer : ITypeSerializer<ShaderInstance, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        if (!node.TryGet<ValueDataNode>("kind", out var kindNode))
            return new ErrorNode(node, "No kind node found.");
        var kindValidation = serializationManager.ValidateNode<ShaderKind>(kindNode, context);
        if (kindValidation is ErrorNode)
        {
            return kindValidation;
        }

        var kind = serializationManager.Read<ShaderKind>(kindNode, context);
        switch (kind)
        {
            case ShaderKind.Source:
                if (!node.TryGet<ValueDataNode>("path", out var pathNode))
                    return new ErrorNode(node, "No path node found.");
                var pathValidation = serializationManager.ValidateNode<ResourcePath>(pathNode, context);
                if (pathValidation is ErrorNode)
                {
                    return pathValidation;
                }
                var path = serializationManager.Read<ResourcePath>(pathNode, context);
                var shaderSource = dependencies.Resolve<IResourceCache>().GetResource<ShaderSourceResource>(path);

                if (node.TryGet<MappingDataNode>("params", out var paramsNode))
                {
                    foreach (var (key, value) in paramsNode)
                    {
                        var name = ((ValueDataNode)key).Value;
                        if (!shaderSource.ParsedShader.Uniforms.TryGetValue(name, out var uniformDefinition))
                        {
                            return new ErrorNode(value, $"Shader param '{name}' does not exist on shader '{path}'");
                        }

                        try
                        {
                            _parseUniformValue(((ValueDataNode)value).Value, uniformDefinition.Type.Type);
                        }
                        catch (NotSupportedException)
                        {
                            return new ErrorNode(value, "Failed parsing uniform value");
                        }
                    }
                }

                break;

            case ShaderKind.Canvas:
                if(node.TryGet<ValueDataNode>("light_mode", out var modeNode))
                {
                    switch (modeNode.Value)
                    {
                        case "normal":
                            break;
                        case "unshaded":
                            break;
                        default:
                            return new ErrorNode(modeNode, $"Invalid light mode: '{modeNode.Value}'");
                    }
                }

                if(node.TryGet<ValueDataNode>("blend_mode", out var blendNode)){
                    switch (blendNode.Value)
                    {
                        case "mix":
                            break;
                        case "add":
                            break;
                        case "subtract":
                            break;
                        case "multiply":
                            break;
                        default:
                            return new ErrorNode(blendNode, $"Invalid blend mode: '{blendNode.Value}'");
                    }
                }
                break;
        }

        return new ValidatedValueNode(node);
    }

    public ShaderInstance Read(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null,
        ShaderInstance? _ = default)
    {
        var kind = serializationManager.Read<ShaderKind>(node["kind"], context, skipHook);

        ShaderInstance instance;

        switch (kind)
        {
            case ShaderKind.Source:
                if (!node.TryGet<ValueDataNode>("path", out var pathNode)) throw new InvalidOperationException();
                var path = serializationManager.Read<ResourcePath>(pathNode, context, skipHook);
                var shaderSource = dependencies.Resolve<IResourceCache>().GetResource<ShaderSourceResource>(path);

                Dictionary<string, object>? shaderParams = null;
                if (node.TryGet<MappingDataNode>("params", out var paramsNode))
                {
                    shaderParams = new Dictionary<string, object>();
                    foreach (var (key, value) in paramsNode)
                    {
                        var name = ((ValueDataNode)key).Value;
                        if (!shaderSource.ParsedShader.Uniforms.TryGetValue(name, out var uniformDefinition))
                        {
                            Logger.ErrorS("shader", "Shader param '{0}' does not exist on shader '{1}'", name, path);
                            continue;
                        }

                        shaderParams.Add(name, _parseUniformValue(((ValueDataNode)value).Value, uniformDefinition.Type.Type));
                    }
                }

                instance = IoCManager.Resolve<IClydeInternal>().InstanceShader(shaderSource.ClydeHandle);
                if (shaderParams != null)
                    _applyDefaultParameters(instance, shaderParams);
                break;

            case ShaderKind.Canvas:
                var source = "";

                if(node.TryGet<ValueDataNode>("light_mode", out var modeNode))
                {
                    switch (modeNode.Value)
                    {
                        case "normal":
                            break;

                        case "unshaded":
                            source += "light_mode unshaded;\n";
                            break;

                        default:
                            throw new InvalidOperationException($"Invalid light mode: '{modeNode.Value}'");
                    }
                }

                if(node.TryGet<ValueDataNode>("blend_mode", out var blendNode)){
                    switch (blendNode.Value)
                    {
                        case "mix":
                            source += "blend_mode mix;\n";
                            break;

                        case "add":
                            source += "blend_mode add;\n";
                            break;

                        case "subtract":
                            source += "blend_mode subtract;\n";
                            break;

                        case "multiply":
                            source += "blend_mode multiply;\n";
                            break;

                        default:
                            throw new InvalidOperationException($"Invalid blend mode: '{blendNode.Value}'");
                    }
                }

                source += "void fragment() {\n    COLOR = zTexture(UV);\n}";

                var preset = ShaderParser.Parse(source, dependencies.Resolve<IResourceCache>());
                var clyde = IoCManager.Resolve<IClydeInternal>();
                instance = clyde.InstanceShader(clyde.LoadShader(preset,
                    $"canvas_preset_{((ValueDataNode)node["id"]).Value}"));
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }


        if (node.TryGet<MappingDataNode>("stencil", out var stencilMapping))
        {
            var stencilData = serializationManager.Read<StencilData>(stencilMapping, context, skipHook);
            instance.StencilTestEnabled = true;
            instance.StencilRef = stencilData.StencilRef;
            instance.StencilFunc = stencilData.StencilFunc;
            instance.StencilOp = stencilData.StencilOp;
            instance.StencilReadMask = stencilData.ReadMask;
            instance.StencilWriteMask = stencilData.WriteMask;
        }

        instance.MakeImmutable();

        return instance;
    }

    public DataNode Write(ISerializationManager serializationManager, ShaderInstance value,
        IDependencyCollection dependencies, bool alwaysWrite = false, ISerializationContext? context = null)
    {
        throw new NotSupportedException();
    }

    public ShaderInstance Copy(ISerializationManager serializationManager, ShaderInstance source,
        ShaderInstance target, bool skipHook, ISerializationContext? context = null)
    {
        return source;
    }

    private static object _parseUniformValue(YamlNode node, ShaderDataType dataType)
    {
        switch (dataType)
        {
            case ShaderDataType.Bool:
                return node.AsBool();
            case ShaderDataType.Int:
                return node.AsInt();
            case ShaderDataType.IVec2:
                return node.AsVector2i();
            case ShaderDataType.Float:
                return node.AsFloat();
            case ShaderDataType.Vec2:
                return node.AsVector2();
            case ShaderDataType.Vec3:
                return node.AsVector3();
            case ShaderDataType.Vec4:
                try
                {
                    return node.AsColor();
                }
                catch
                {
                    return node.AsVector4();
                }
            default:
                throw new NotSupportedException("Unsupported uniform type.");
        }
    }

    private void _applyDefaultParameters(ShaderInstance instance, Dictionary<string, object> shaderParams)
    {
        foreach (var (key, value) in shaderParams)
        {
            switch (value)
            {
                case int i:
                    instance.SetParameter(key, i);
                    break;
                case Vector2i i:
                    instance.SetParameter(key, i);
                    break;
                case float i:
                    instance.SetParameter(key, i);
                    break;
                case Vector2 i:
                    instance.SetParameter(key, i);
                    break;
                case Vector3 i:
                    instance.SetParameter(key, i);
                    break;
                case Vector4 i:
                    instance.SetParameter(key, i);
                    break;
                case Color i:
                    instance.SetParameter(key, i);
                    break;
                case bool i:
                    instance.SetParameter(key, i);
                    break;
            }
        }
    }

    [DataDefinition]
    private sealed class StencilData
    {
        [DataField("ref")] public int StencilRef;

        [DataField("op")] public StencilOp StencilOp;

        [DataField("func")] public StencilFunc StencilFunc;

        [DataField("readMask")] public int ReadMask = unchecked((int) uint.MaxValue);

        [DataField("writeMask")] public int WriteMask = unchecked((int) uint.MaxValue);
    }

    private enum ShaderKind : byte
    {
        Source,
        Canvas
    }
}
