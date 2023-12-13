using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes;

public partial class PrototypeManager
{
    public Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResPath path) => ValidateDirectory(path, out _);

    public Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResPath path,
        out Dictionary<Type, HashSet<string>> protos)
    {
        var streams = Resources.ContentFindFiles(path).ToList().AsParallel()
            .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."));

        var dict = new Dictionary<string, HashSet<ErrorNode>>();
        var prototypes = new Dictionary<Type, Dictionary<string, PrototypeValidationData>>();

        foreach (var resourcePath in streams)
        {
            using var reader = ReadFile(resourcePath);

            if (reader == null)
            {
                continue;
            }

            var yamlStream = new YamlStream();
            yamlStream.Load(reader);

            foreach (var doc in yamlStream.Documents)
            {
                var rootNode = (YamlSequenceNode)doc.RootNode;
                foreach (YamlMappingNode node in rootNode.Cast<YamlMappingNode>())
                {
                    var typeId = node.GetNode("type").AsString();
                    if (_ignoredPrototypeTypes.Contains(typeId))
                        continue;

                    if (!_kindNames.TryGetValue(typeId, out var type))
                    {
                        throw new PrototypeLoadException($"Unknown prototype type: '{typeId}'");
                    }

                    var mapping = node.ToDataNodeCast<MappingDataNode>();
                    var id = mapping.Get<ValueDataNode>("id").Value;

                    var data = new PrototypeValidationData(mapping, resourcePath.ToString());
                    mapping.Remove("type");

                    if (prototypes.GetOrNew(type).TryAdd(id, data))
                        continue;

                    var error = new ErrorNode(mapping, $"Found dupe prototype ID of {id} for {type}");
                    dict.GetOrNew(data.File).Add(error);
                }
            }
        }

        foreach (var (type, instances) in prototypes)
        {
            foreach (var data in instances.Values)
            {
                EnsurePushed(data, instances, type);
                if (data.Mapping.TryGet("abstract", out ValueDataNode? abstractNode)
                    && bool.Parse(abstractNode.Value))
                {
                    continue;
                }

                var result = _serializationManager.ValidateNode(type, data.Mapping).GetErrors().ToHashSet();
                if (result.Count > 0)
                    dict.GetOrNew(data.File).UnionWith(result);
            }
        }

        protos = new(prototypes.Count);
        foreach (var (type, typeDict) in prototypes)
        {
            protos[type] = typeDict.Keys.ToHashSet();
        }

        return dict;
    }

    public Dictionary<Type, Dictionary<string, HashSet<ErrorNode>>> ValidateAllPrototypesSerializable(ISerializationContext? ctx)
    {
        var result = new Dictionary<Type, Dictionary<string, HashSet<ErrorNode>>>();
        var dict = new Dictionary<string, HashSet<ErrorNode>>();

        foreach (var (type, kinds) in _kinds)
        {
            foreach (var instance in kinds.Instances.Values)
            {
                DebugTools.Assert(type == instance.GetType());
                var errorNodes = ValidateProto(type, instance, ctx, out var caughtException);
                if (errorNodes.Count > 0)
                    dict.GetOrNew(instance.ID).UnionWith(errorNodes);

                // Avoid tests taking forever as they thrown one exception per prototype.
                if (caughtException)
                    break;
            }

            if (dict.Count > 0)
            {
                result[type] = dict;
                dict = new();
            }
        }

        return result;
    }

    private HashSet<ErrorNode> ValidateProto(Type type, IPrototype instance, ISerializationContext? ctx,
        out bool caughtException)
    {
        caughtException = false;
        DataNode node;
        try
        {
            node = _serializationManager.WriteValue(type, instance, alwaysWrite: true, context:ctx);
        }
        catch (Exception e)
        {
            caughtException = true;
            var msg = $"Caught exception while writing. Exception: {e}";
            return new() { new ErrorNode(new ValueDataNode(""), msg) };
        }

        try
        {
            return _serializationManager.ValidateNode(type, node, context:ctx)
                .GetErrors()
                .ToHashSet();
        }
        catch (Exception e)
        {
            caughtException = true;
            var msg = $"Caught exception while validating. Exception: {e}";
            return new() { new ErrorNode(new ValueDataNode(""), msg) };
        }
    }

    private sealed class PrototypeValidationData
    {
        public MappingDataNode Mapping;
        public readonly string File;
        public bool Pushed;

        public PrototypeValidationData(MappingDataNode mapping, string file)
        {
            File = file;
            Mapping = mapping;
        }
    }

    private void EnsurePushed(
        PrototypeValidationData data,
        Dictionary<string, PrototypeValidationData> prototypes,
        Type type)
    {
        if (data.Pushed)
            return;

        data.Pushed = true;

        if (!data.Mapping.TryGet(ParentDataFieldAttribute.Name, out var parentNode))
            return;

        var parents = _serializationManager.Read<string[]>(parentNode, notNullableOverride: true);
        var parentNodes = new MappingDataNode[parents.Length];

        foreach (var parentId in parents)
        {
            var parent = prototypes[parentId];
            EnsurePushed(parent, prototypes, type);

            for (var i = 0; i < parents.Length; i++)
            {
                parentNodes[i] = parent.Mapping;
            }
        }

        data.Mapping = _serializationManager.PushCompositionWithGenericNode(
            type,
            parentNodes,
            data.Mapping);
    }
}
