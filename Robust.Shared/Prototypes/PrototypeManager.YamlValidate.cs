using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using BindingFlags = System.Reflection.BindingFlags;

namespace Robust.Shared.Prototypes;

public partial class PrototypeManager
{
    public (Dictionary<string, HashSet<ErrorNode>> YamlErrors, List<string> staticIdErrors)
        ValidateDirectory(ResPath path, bool validateAssemblyIds = true)
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
                    if (!_kindNames.TryGetValue(typeId, out var type))
                    {
                        if (_ignoredPrototypeTypes.Contains(typeId))
                            continue;

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

        var staticIdErrors = new List<string>();
        if (!validateAssemblyIds)
            return (dict, staticIdErrors);

        const BindingFlags flags =
            BindingFlags.DeclaredOnly
            | BindingFlags.Static
            | BindingFlags.Instance
            | BindingFlags.NonPublic
            | BindingFlags.Public;

        foreach (var t in _reflectionManager.FindAllTypes())
        {
            object? obj = null;

            foreach (var field in t.GetFields(flags))
            {
                Type prototypeKind;
                DataFieldAttribute? dataDef = null;

                var attrib = field.GetCustomAttribute(typeof(PrototypeIdAttribute<>), false);
                if (attrib != null)
                {
                    prototypeKind = attrib.GetType().GetGenericArguments().First();
                }
                else
                {
                    // Maybe this is a data-field with a prototype id serializer
                    if (!field.TryGetCustomAttribute(out dataDef)
                        || dataDef.CustomTypeSerializer == null
                        || !dataDef.CustomTypeSerializer.IsGenericType
                        || dataDef.CustomTypeSerializer.GetGenericTypeDefinition() != typeof(PrototypeIdSerializer<>))
                    {
                        continue;
                    }

                    prototypeKind = dataDef.CustomTypeSerializer.GetGenericArguments().First();
                }

                if (!field.IsStatic && obj == null)
                {
                    // This is an  instance field. So we will create an instance and try to get the default value.
                    try
                    {
                        obj = Activator.CreateInstance(t);
                    }
                    catch
                    {
                        staticIdErrors.Add($"Prototype id field failed validation. could not create instance to validate default value. Field: {field} in {t.FullName}");
                        continue;
                    }
                }

                if (field.FieldType != typeof(string))
                {
                    staticIdErrors.Add($"Prototype id field failed validation. Field is not a string. Field: {field} in {t.FullName}");
                    continue;
                }

                var value = field.GetValue(obj);
                if (value is not string id || string.IsNullOrWhiteSpace(id))
                {
                    if (field.IsStatic)
                    {
                        // Const fields should always have a valid value.
                        staticIdErrors.Add($"Prototype id field failed validation. Static fields must have a value. Field: {field} in {t.FullName}");
                    }
                    else if (value != null && !dataDef!.Required)
                    {
                        // This is an optional data-field, it should either be nullable or have a sane default value.
                        staticIdErrors.Add($"Prototype id field failed validation. Optional data-fields must be nullable or have a default value. Field: {field} in {t.FullName}");
                    }

                    continue;
                }

                if (!prototypes.TryGetValue(prototypeKind, out var instances))
                {
                    staticIdErrors.Add($"Prototype id field failed validation. Unknown prototype kind. Field: {field} in {t.FullName}");
                    continue;
                }

                if (!instances.ContainsKey(id))
                {
                    staticIdErrors.Add($"Prototype id field failed validation. Unknown prototype: {id}. Field: {field} in {t.FullName}");
                }
            }
        }

        return (dict, staticIdErrors);
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
