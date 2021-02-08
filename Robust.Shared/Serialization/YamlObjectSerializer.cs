using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization
{
    /// <summary>
    ///     Object serializer that serializes to/from YAML.
    /// </summary>
    public class YamlObjectSerializer : ObjectSerializer
    {
        private const string TagSkipTag = "SRZ_DO_NOT_TAG";

        private static readonly Dictionary<Type, TypeSerializer> _typeSerializers;
        public static IReadOnlyDictionary<Type, TypeSerializer> TypeSerializers => _typeSerializers;
        private static readonly StructSerializer _structSerializer;

        private YamlMappingNode? WriteMap;
        private List<YamlMappingNode>? ReadMaps;
        private Context? _context;

        static YamlObjectSerializer()
        {
            _structSerializer = new StructSerializer();
            _typeSerializers = new Dictionary<Type, TypeSerializer>
            {
                { typeof(Color), new ColorSerializer() },
                { typeof(Vector2), new Vector2Serializer() },
                { typeof(Vector3), new Vector3Serializer() },
                { typeof(Vector4), new Vector4Serializer() },
                { typeof(Angle), new AngleSerializer() },
                { typeof(UIBox2), new UIBox2Serializer() },
                { typeof(Box2), new Box2Serializer() },
                { typeof(ResourcePath), new ResourcePathSerializer() },
                { typeof(GridId), new GridIdSerializer() },
                { typeof(MapId), new MapIdSerializer() },
                { typeof(SpriteSpecifier), new SpriteSpecifierSerializer() },
                { typeof(TimeSpan), new TimeSpanSerializer() },
            };
        }

        // Use NewReader or NewWriter instead.
        private YamlObjectSerializer()
        {
        }

        /// <summary>
        ///     Creates a new serializer to be used for reading from YAML data.
        /// </summary>
        /// <param name="readMap">
        ///     The YAML mapping to read data from.
        /// </param>
        /// <param name="context">
        ///     An optional context that can provide additional capabitilies such as caching and custom type serializers.
        /// </param>
        public static YamlObjectSerializer NewReader(YamlMappingNode readMap, Context? context = null)
        {
            return NewReader(new List<YamlMappingNode>(1) { readMap }, context);
        }

        public static YamlObjectSerializer NewReader(YamlMappingNode readMap, Type deserializingType,
            Context? context = null)
        {
            var r = NewReader(readMap, context);
            r.SetDeserializingType(deserializingType);
            return r;
        }

        /// <summary>
        ///     Creates a new serializer to be used for reading from YAML data.
        /// </summary>
        /// <param name="readMaps">
        ///     A list of maps to read from. The first list will be used first,
        ///     then the second if the first does not contain a specific key, and so on.
        /// </param>
        /// <param name="context">
        ///     An optional context that can provide additional capabilities such as caching and custom type serializers.
        /// </param>
        public static YamlObjectSerializer NewReader(List<YamlMappingNode> readMaps, Context? context = null)
        {
            return new()
            {
                ReadMaps = readMaps,
                _context = context,
                Reading = true,
            };
        }

        /// <summary>
        ///     Creates a new serializer to be used from writing into YAML data.
        /// </summary>
        /// <param name="writeMap">
        ///     The mapping to write into.
        ///     Gets modified directly in place.
        /// </param>
        /// <param name="context">
        ///     An optional context that can provide additional capabitilies such as caching and custom type serializers.
        /// </param>
        public static YamlObjectSerializer NewWriter(YamlMappingNode writeMap, Context? context = null)
        {
            return new()
            {
                WriteMap = writeMap,
                _context = context,
                Reading = false,
            };
        }

        // TODO: Theoretical optimization.
        // Might be a good idea to make DataField<T> use caching for value types without references too.
        /// <inheritdoc />
        public override void DataField<T>(ref T value, string name, T defaultValue, WithFormat<T> format, bool alwaysWrite = false)
        {
            if (Reading) // read
            {
                foreach (var map in ReadMaps!)
                {
                    if (map.TryGetNode(name, out var node))
                    {
                        var customFormatter = format.GetYamlSerializer();
                        value = (T)customFormatter.NodeToType(typeof(T), node, this);
                        return;
                    }
                }
                value = defaultValue;
                return;
            }
            else // write
            {
                // don't write if value is null or default
                if (!alwaysWrite && IsValueDefault(name, value, defaultValue, format))
                    return;

                // if value AND defaultValue are null then IsValueDefault above will abort.
                var customFormatter = format.GetYamlSerializer();
                var key = name;
                var val = value == null ? customFormatter.TypeToNode(defaultValue!, this) : customFormatter.TypeToNode(value, this);

                // write the concrete type tag
                AssignTag(typeof(T), value, defaultValue, val);

                WriteMap!.Add(key, val);
            }
        }

        private static void AssignTag<T>(Type t, T value, T defaultValue, YamlNode node)
        {
            // This TagSkipTag thing is a hack
            // to get the serializer to SKIP writing type-tags
            // for some special cases like serializing IReadOnlyList<T>.
            // If the skip tag is set, we clear it and don't set a tag.
            if (node.Tag == TagSkipTag)
            {
                node.Tag = null;
                return;
            }

            if (t.IsAbstract || t.IsInterface)
            {
                var concreteType = value == null ? defaultValue!.GetType() : value.GetType();
                node.Tag = $"!type:{concreteType.Name}";
            }
        }

        /// <inheritdoc />
        public override void DataFieldCached<T>(ref T value, string name, T defaultValue, WithFormat<T> format, bool alwaysWrite = false)
        {
            if (Reading) // read
            {
                if (_context != null && _context.TryGetCachedField<T>(name, out var theValue))
                {
                    // Itermediate field so value doesn't get reset to default(T) if this fails.
                    value = theValue;
                    return;
                }
                foreach (var map in ReadMaps!)
                {
                    if (map.TryGetNode(name, out var node))
                    {
                        var customFormatter = format.GetYamlSerializer();
                        value = (T)customFormatter.NodeToType(typeof(T), node, this);
                        _context?.SetCachedField(name, value);
                        return;
                    }
                }
                value = defaultValue;
                _context?.SetCachedField(name, value);
                return;
            }
            else // write
            {
                DataField(ref value, name, defaultValue, format, alwaysWrite);
            }
        }

        /// <inheritdoc />
        public override void DataField<TTarget, TSource>(
            ref TTarget value,
            string name,
            TTarget defaultValue,
            ReadConvertFunc<TTarget, TSource> ReadConvertFunc,
            WriteConvertFunc<TTarget, TSource>? WriteConvertFunc = null,
            bool alwaysWrite = false)
        {
            if (Reading)
            {
                foreach (var map in ReadMaps!)
                {
                    if (map.TryGetNode(name, out var node))
                    {
                        value = ReadConvertFunc((TSource)NodeToType(typeof(TSource), node));
                        return;
                    }
                }
                value = defaultValue;
            }
            else
            {
                if (WriteConvertFunc == null)
                {
                    // TODO: More verbosity diagnostics.
                    Logger.WarningS(LogCategory, "Field '{0}' not written due to lack of WriteConvertFunc.", name);
                    return;
                }

                // don't write if value is null or default
                if (!alwaysWrite && IsValueDefault(name, value, defaultValue, WithFormat<TTarget>.NoFormat))
                {
                    return;
                }

                var key = name;
                var val = value == null ? TypeToNode(WriteConvertFunc(defaultValue!)) : TypeToNode(WriteConvertFunc(value!));

                // write the concrete type tag
                AssignTag(typeof(TTarget), value, defaultValue, val);

                WriteMap!.Add(key, val);
            }
        }

        /// <inheritdoc />
        public override void DataFieldCached<TTarget, TSource>(
            ref TTarget value,
            string name,
            TTarget defaultValue,
            ReadConvertFunc<TTarget, TSource> ReadConvertFunc,
            WriteConvertFunc<TTarget, TSource>? WriteConvertFunc = null,
            bool alwaysWrite = false)
        {
            if (Reading)
            {
                if (_context != null && _context.TryGetCachedField<TTarget>(name, out var theValue))
                {
                    // Itermediate field so value doesn't get reset to default(T) if this fails.
                    value = theValue;
                    return;
                }
                foreach (var map in ReadMaps!)
                {
                    if (map.TryGetNode(name, out var node))
                    {
                        value = ReadConvertFunc((TSource)NodeToType(typeof(TSource), node));
                        _context?.SetCachedField(name, value);
                        return;
                    }
                }
                value = defaultValue;
                _context?.SetCachedField(name, value);
            }
            else
            {
                DataField(ref value, name, defaultValue, ReadConvertFunc, WriteConvertFunc, alwaysWrite);
            }
        }

        /// <inheritdoc />
        public override T ReadDataField<T>(string name, T defaultValue)
        {
            if (!Reading)
            {
                throw new InvalidOperationException("Cannot use ReadDataField while not reading.");
            }

            foreach (var map in ReadMaps!)
            {
                if (map.TryGetNode(name, out var node))
                {
                    return (T)NodeToType(typeof(T), node);

                }
            }
            return defaultValue;
        }

        /// <inheritdoc />
        public override T ReadDataFieldCached<T>(string name, T defaultValue)
        {
            if (!Reading)
            {
                throw new InvalidOperationException("Cannot use ReadDataField while not reading.");
            }

            if (_context != null && _context.TryGetCachedField<T>(name, out var val))
            {
                return val;
            }

            foreach (var map in ReadMaps!)
            {
                if (map.TryGetNode(name, out var node))
                {
                    val = (T)NodeToType(typeof(T), node);
                    _context?.SetCachedField(name, val);
                    return val;
                }
            }
            _context?.SetCachedField(name, defaultValue);
            return defaultValue;
        }

        /// <inheritdoc />
        public override bool TryReadDataField<T>(string name, WithFormat<T> format, [MaybeNullWhen(false)] out T value)
        {
            if (!Reading)
            {
                throw new InvalidOperationException("Cannot use ReadDataField while not reading.");
            }

            foreach (var map in ReadMaps!)
            {
                if (map.TryGetNode(name, out var node))
                {
                    var customFormatter = format.GetYamlSerializer();
                    value = (T)customFormatter.NodeToType(typeof(T), node, this);
                    return true;
                }
            }
            value = default;
            return false;
        }

        public override bool TryReadDataFieldCached<T>(string name, WithFormat<T> format, [MaybeNullWhen(false)] out T value)
        {
            if (!Reading)
            {
                throw new InvalidOperationException("Cannot use ReadDataField while not reading.");
            }

            if (_context != null && _context.TryGetCachedField(name, out value))
            {
                return true;
            }

            foreach (var map in ReadMaps!)
            {
                if (map.TryGetNode(name, out var node))
                {
                    var customFormatter = format.GetYamlSerializer();
                    value = (T)customFormatter.NodeToType(typeof(T), node, this);
                    _context?.SetCachedField(name, value);
                    return true;
                }
            }
            value = default;
            return false;
        }


        /// <inheritdoc />
        public override void DataReadFunction<T>(string name, T defaultValue, ReadFunctionDelegate<T> func)
        {
            if (!Reading) return;

            foreach (var map in ReadMaps!)
            {
                if (map.TryGetNode(name, out var node))
                {
                    func((T)NodeToType(typeof(T), node));
                    return;
                }
            }

            func(defaultValue);
        }

        /// <inheritdoc />
        public override void DataWriteFunction<T>(string name, T defaultValue, WriteFunctionDelegate<T> func, bool alwaysWrite = false)
        {
            if (Reading) return;

            var value = func.Invoke();

            // don't write if value is null or default
            if (!alwaysWrite && IsValueDefault(name, value, defaultValue, WithFormat<T>.NoFormat))
                return;

            var key = name;
            var val = value == null ? TypeToNode(defaultValue!) : TypeToNode(value);

            // write the concrete type tag
            AssignTag(typeof(T), value, defaultValue, val);

            WriteMap!.Add(key, val);
        }

        /// <inheritdoc />
        public override void SetCacheData(string key, object value)
        {
            _context?.SetDataCache(key, value);
        }

        /// <inheritdoc />
        public override T GetCacheData<T>(string key)
        {
            if (_context != null && _context.TryGetDataCache(key, out var value))
            {
                return (T) value!;
            }
            throw new KeyNotFoundException();
        }

        /// <inheritdoc />
        public override bool TryGetCacheData<T>(string key, [MaybeNullWhen(false)] out T data)
        {
            if (_context != null && _context.TryGetDataCache(key, out var value))
            {
                data = (T) value!;
                return true;
            }

            data = default;
            return false;
        }

        public object NodeToType(Type type, YamlNode node)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            // special snowflake string
            if (type == typeof(String))
                return node.ToString();

            // val primitives
            if (underlyingType.IsPrimitive || underlyingType == typeof(decimal))
            {
                return StringToType(type, node.ToString());
            }

            // array
            if (type.IsArray)
            {
                var listNode = (YamlSequenceNode)node;
                var newArray = (Array)Activator.CreateInstance(type, listNode.Children.Count)!;

                var idx = 0;
                foreach (var entryNode in listNode)
                {
                    var value = NodeToType(type.GetElementType()!, entryNode);
                    newArray.SetValue(value, idx++);
                }

                return newArray;
            }

            // val enum
            if (type.IsEnum)
                return Enum.Parse(type, node.ToString());

            // IReadOnlyList<T>/IReadOnlyCollection<T>
            if (TryGenericReadOnlyCollectionType(type, out var collectionType))
            {
                var listNode = (YamlSequenceNode)node;
                var elems = listNode.Children;
                // Deserialize to an array because that is much more efficient, and allowed.
                var newList = (IList)Array.CreateInstance(collectionType, elems.Count);

                for (var i = 0; i < elems.Count; i++)
                {
                    newList[i] = NodeToType(collectionType, elems[i]);
                }

                return newList;
            }

            // List<T>
            if (TryGenericListType(type, out var listType))
            {
                var listNode = (YamlSequenceNode)node;
                var newList = (IList)Activator.CreateInstance(type, listNode.Children.Count)!;

                foreach (var entryNode in listNode)
                {
                    var value = NodeToType(listType, entryNode);
                    newList.Add(value);
                }

                return newList;
            }

            // Dictionary<K,V>/IReadOnlyDictionary<K,V>
            if (TryGenericReadDictType(type, out var keyType, out var valType, out var dictType))
            {
                var dictNode = (YamlMappingNode)node;
                var newDict = (IDictionary)Activator.CreateInstance(dictType, dictNode.Children.Count)!;

                foreach (var kvEntry in dictNode.Children)
                {
                    var keyValue = NodeToType(keyType, kvEntry.Key);
                    var valValue = NodeToType(valType, kvEntry.Value);

                    newDict.Add(keyValue, valValue);
                }

                return newDict;
            }

            // HashSet<T>
            if (TryGenericHashSetType(type, out var setType))
            {
                var nodes = ((YamlSequenceNode) node).Children;
                var valuesArray = Array.CreateInstance(setType, new[] {nodes.Count})!;

                for (var i = 0; i < nodes.Count; i++)
                {
                    var value = NodeToType(setType, nodes[i]);
                    valuesArray.SetValue(value, i);
                }

                var newSet = Activator.CreateInstance(type, valuesArray)!;

                return newSet;
            }

            // KeyValuePair<K, V>
            if (TryGenericKeyValuePairType(type, out var kvpKeyType, out var kvpValType))
            {
                var pairType = typeof(KeyValuePair<,>).MakeGenericType(kvpKeyType, kvpValType);
                var pairNode = (YamlMappingNode) node;

                switch (pairNode.Children.Count)
                {
                    case 0:
                        return Activator.CreateInstance(pairType)!;
                    case 1:
                    {
                        using var enumerator = pairNode.GetEnumerator();
                        enumerator.MoveNext();
                        var yamlPair = enumerator.Current;
                        var keyValue = NodeToType(kvpKeyType, yamlPair.Key);
                        var valValue = NodeToType(kvpValType, yamlPair.Value);
                        var pair = Activator.CreateInstance(pairType, keyValue, valValue)!;

                        return pair;
                    }
                    default:
                        throw new InvalidOperationException(
                            $"Cannot read KeyValuePair from mapping node with more than one child.");
                }
            }

            // Hand it to the context.
            if (_context != null && _context.TryNodeToType(node, type, out var contextObj))
            {
                return contextObj;
            }

            // custom TypeSerializer
            if (_typeSerializers.TryGetValue(type, out var serializer))
                return serializer.NodeToType(type, node, this);

            // IExposeData.
            if (typeof(IExposeData).IsAssignableFrom(type))
            {
                if (!(node is YamlMappingNode mapNode))
                {
                    throw new InvalidOperationException($"Cannot read from IExposeData on non-mapping node. Type: '{type}'");
                }

                var concreteType = type;
                if (type.IsAbstract || type.IsInterface)
                {
                    var tag = node.Tag;
                    if (string.IsNullOrWhiteSpace(tag))
                        throw new YamlException($"Type '{type}' is abstract, but there is no yaml tag for the concrete type.");

                    if (tag.StartsWith("!type:"))
                    {
                        concreteType = ResolveConcreteType(type, tag["!type:".Length..]);
                    }
                    else
                    {
                        throw new YamlException("Malformed type tag.");
                    }
                }

                var instance = (IExposeData)Activator.CreateInstance(concreteType)!;
                // TODO: Might be worth it to cut down on allocations here by using ourselves instead of creating a fork.
                // Seems doable.
                if (_context != null)
                {
                    _context.StackDepth++;
                }
                var fork = NewReader(mapNode, _context);
                fork.CurrentType = concreteType;
                if (_context != null)
                {
                    _context.StackDepth--;
                }
                instance.ExposeData(fork);
                return instance;
            }

            // ISelfSerialize
            if (typeof(ISelfSerialize).IsAssignableFrom(type))
            {
                var instance = (ISelfSerialize)Activator.CreateInstance(type)!;
                instance.Deserialize(node.ToString());
                return instance;
            }

            // other val (struct)
            if (type.IsValueType)
                return _structSerializer.NodeToType(type, (YamlMappingNode)node, this);

            // ref type that isn't a custom TypeSerializer
            throw new ArgumentException($"Type {type.FullName} is not supported.", nameof(type));
        }

        public T NodeToType<T>(YamlNode node)
        {
            return (T) NodeToType(typeof(T), node);
        }

        private static Type ResolveConcreteType(Type baseType, string typeName)
        {
            var reflection = IoCManager.Resolve<IReflectionManager>();
            var type = reflection.YamlTypeTagLookup(baseType, typeName);
            if (type == null)
            {
                throw new YamlException($"Type '{baseType}' is abstract, but could not find concrete type '{typeName}'.");
            }

            return type;
        }

        public YamlNode TypeToNode(object obj)
        {
            // special snowflake string
            if (obj is string s)
                return s;

            var type = obj.GetType();
            type = Nullable.GetUnderlyingType(type) ?? type;

            // val primitives and val enums
            if (type.IsPrimitive || type.IsEnum || type == typeof(decimal))
            {
                // All primitives and enums implement IConvertible.
                // Need it for the culture overload.
                var convertible = (IConvertible) obj;
                return convertible.ToString(CultureInfo.InvariantCulture);
            }

            // array
            if (type.IsArray)
            {
                var sequence = new YamlSequenceNode();
                var element = type.GetElementType()!;

                foreach (var entry in (IEnumerable) obj)
                {
                    if (entry == null)
                    {
                        continue;
                        throw new ArgumentException("Cannot serialize null value inside array.");
                    }

                    var entryNode = TypeToNode(entry);

                    // write the concrete type tag
                    AssignTag<object?>(element, entry, null, entryNode);

                    sequence.Add(entryNode);
                }

                return sequence;
            }

            // List<T>/IReadOnlyCollection<T>/IReadOnlyList<T>
            if (TryGenericListType(type, out var listType) || TryGenericReadOnlyCollectionType(type, out listType))
            {
                var node = new YamlSequenceNode();
                node.Tag = TagSkipTag;

                foreach (var entry in (IEnumerable)obj)
                {
                    if (entry == null)
                    {
                        throw new ArgumentException("Cannot serialize null value inside list.");
                    }

                    var entryNode = TypeToNode(entry);

                    // write the concrete type tag
                    AssignTag<object?>(listType, entry, null, entryNode);

                    node.Add(entryNode);
                }

                return node;
            }

            // Dictionary<K,V>
            if (TryGenericDictType(type, out var keyType, out var valType)
                || TryGenericReadOnlyDictType(type, out keyType, out valType))
            {
                var node = new YamlMappingNode();
                node.Tag = TagSkipTag;

                foreach (var oEntry in (IDictionary)obj)
                {
                    var entry = (DictionaryEntry) oEntry!;
                    var keyNode = TypeToNode(entry.Key);
                    if (entry.Value == null)
                    {
                        throw new ArgumentException("Cannot serialize null value inside dictionary.");
                    }

                    var valNode = TypeToNode(entry.Value);

                    // write the concrete type tag
                    AssignTag<object?>(valType, entry, null, valNode);

                    node.Add(keyNode, valNode);
                }

                return node;
            }

            // HashSet<T>
            if (TryGenericHashSetType(type, out var setType))
            {
                var node = new YamlSequenceNode();
                node.Tag = TagSkipTag;

                foreach (var entry in (IEnumerable)obj)
                {
                    if (entry == null)
                    {
                        throw new ArgumentException("Cannot serialize null value inside hashset.");
                    }

                    var entryNode = TypeToNode(entry);

                    // write the concrete type tag
                    AssignTag<object?>(setType, entry, null, entryNode);

                    node.Add(entryNode);
                }

                return node;
            }

            if (TryGenericKeyValuePairType(type, out var kvpKeyType, out var kvpValType))
            {
                var node = new YamlMappingNode {Tag = TagSkipTag};
                dynamic pair = obj;
                var keyNode = TypeToNode(pair.Key);
                var valNode = TypeToNode(pair.Value);

                // write the concrete type tag
                AssignTag<object?>(kvpValType, pair, null, valNode);

                node.Add(keyNode, valNode);

                return node;
            }

            // Hand it to the context.
            if (_context != null && _context.TryTypeToNode(obj, out var contextNode))
            {
                return contextNode;
            }

            // custom TypeSerializer
            if (_typeSerializers.TryGetValue(type, out var serializer))
                return serializer.TypeToNode(obj, this);

            // IExposeData.
            if (obj is IExposeData exposable)
            {
                var mapping = new YamlMappingNode();
                if (_context != null)
                {
                    _context.StackDepth++;
                }
                var fork = NewWriter(mapping, _context);
                fork.CurrentType = type;
                if (_context != null)
                {
                    _context.StackDepth--;
                }
                exposable.ExposeData(fork);
                return mapping;
            }

            // ISelfSerialize
            if (typeof(ISelfSerialize).IsAssignableFrom(type))
            {
                var instance = (ISelfSerialize) obj!;
                return instance.Serialize();
            }

            // other val (struct)
            if (type.IsValueType)
                return _structSerializer.TypeToNode(obj, this);

            // ref type that isn't a custom TypeSerializer
            throw new ArgumentException($"Type {type.FullName} is not supported.", nameof(obj));
        }

        bool IsValueDefault<T>(string field, T value, T providedDefault, WithFormat<T> format)
        {
            if ((value != null || providedDefault == null) && (value == null || IsSerializedEqual(value, providedDefault)))
            {
                return true;
            }

            if (_context != null)
            {
                return _context.IsValueDefault(field, value, format);
            }

            return false;

        }

        internal static bool IsSerializedEqual(object? a, object? b)
        {
            var type = a?.GetType();
            if (type != b?.GetType())
            {
                return false;
            }

            if (a == null) // Also implies b is null since it'd have failed the type equality check otherwise.
            {
                return true;
            }

            if (TryGenericListType(type!, out _))
            {
                var listA = (IList) a;
                var listB = (IList) b!;

                if (listA.Count != listB.Count)
                {
                    return false;
                }

                for (var i = 0; i < listA.Count; i++)
                {
                    var elemA = listA[i];
                    var elemB = listB[i];

                    if (!IsSerializedEqual(elemA, elemB))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (TryGenericDictType(type!, out _, out _))
            {
                var dictA = (IDictionary) a;
                var dictB = (IDictionary) b!;

                foreach (var entryMaybe in dictA)
                {
                    var entry = (DictionaryEntry) entryMaybe!;
                    var k = entry.Key;
                    var v = entry.Value;

                    if (!dictB.Contains(k))
                    {
                        return false;
                    }

                    if (!IsSerializedEqual(v, dictB[k]))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (TryGenericHashSetType(type!, out _))
            {
                var setA = ((IEnumerable) a).GetEnumerator();
                var setB = ((IEnumerable) b!).GetEnumerator();

                while (setA.MoveNext())
                {
                    if (!setB.MoveNext())
                    {
                        return false;
                    }

                    if (!IsSerializedEqual(setA.Current, setB.Current))
                    {
                        return false;
                    }
                }

                if (setB.MoveNext())
                {
                    return false;
                }

                return true;
            }

            if (TryGenericKeyValuePairType(type!, out _, out _))
            {
                dynamic tupleA = a;
                dynamic tupleB = b!;

                if (!IsSerializedEqual(tupleA.Key, tupleB.Key))
                {
                    return false;
                }

                if (!IsSerializedEqual(tupleA.Value, tupleB.Value))
                {
                    return false;
                }

                return true;
            }

            if (typeof(IExposeData).IsAssignableFrom(type))
            {
                // Serialize both, see if output matches.
                var testA = new YamlMappingNode();
                var testB = new YamlMappingNode();
                var serA = NewWriter(testA);
                var serB = NewWriter(testB);

                var expA = (IExposeData) a;
                var expB = (IExposeData) b!;

                expA.ExposeData(serA);
                expB.ExposeData(serB);

                // Does deep equality.
                return testA.Equals(testB);
            }

            return a.Equals(b);
        }


        private static object StringToType(Type type, string str)
        {
            var foo = TypeDescriptor.GetConverter(type);
            return foo.ConvertFromInvariantString(str);
        }

        public static void RegisterTypeSerializer(Type type, TypeSerializer serializer)
        {
            if (!_typeSerializers.ContainsKey(type))
                _typeSerializers.Add(type, serializer);
        }

        private static bool TryGenericReadOnlyCollectionType(Type type, [NotNullWhen(true)] out Type? listType)
        {
            if (!type.GetTypeInfo().IsGenericType)
            {
                listType = default;
                return false;
            }

            var baseGeneric = type.GetGenericTypeDefinition();
            var isList = baseGeneric == typeof(IReadOnlyCollection<>) || baseGeneric == typeof(IReadOnlyList<>);

            if (isList)
            {
                listType = type.GetGenericArguments()[0];
                return true;
            }

            listType = default;
            return false;
        }

        private static bool TryGenericListType(Type type, [NotNullWhen(true)] out Type? listType)
        {
            var isList = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

            if (isList)
            {
                listType = type.GetGenericArguments()[0];
                return true;
            }

            listType = default;
            return false;
        }

        private static bool TryGenericReadOnlyDictType(Type type, [NotNullWhen(true)] out Type? keyType, [NotNullWhen(true)] out Type? valType)
        {
            var isDict = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>);

            if (isDict)
            {
                var genArgs = type.GetGenericArguments();
                keyType = genArgs[0];
                valType = genArgs[1];
                return true;
            }

            keyType = default;
            valType = default;
            return false;
        }

        private static bool TryGenericReadDictType(Type type, [NotNullWhen(true)] out Type? keyType,
            [NotNullWhen(true)] out Type? valType, [NotNullWhen(true)] out Type? dictType)
        {
            if (TryGenericDictType(type, out keyType, out valType))
            {
                // Pass through the type directly if it's Dictionary<K,V>.
                // Since that's more efficient.
                dictType = type;
                return true;
            }

            if (TryGenericReadOnlyDictType(type, out keyType, out valType))
            {
                // If it's IReadOnlyDictionary<K,V> we need to make a Dictionary<K,V> type to use to deserialize.
                dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valType);
                return true;
            }

            dictType = default;
            return false;
        }

        private static bool TryGenericDictType(Type type, [NotNullWhen(true)] out Type? keyType, [NotNullWhen(true)] out Type? valType)
        {
            var isDict = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

            if (isDict)
            {
                var genArgs = type.GetGenericArguments();
                keyType = genArgs[0];
                valType = genArgs[1];
                return true;
            }

            keyType = default;
            valType = default;
            return false;
        }

        private static bool TryGenericHashSetType(Type type, [NotNullWhen(true)] out Type? setType)
        {
            var isSet = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>);

            if (isSet)
            {
                setType = type.GetGenericArguments()[0];
                return true;
            }

            setType = default;
            return false;
        }

        private static bool TryGenericKeyValuePairType(
            Type type,
            [NotNullWhen(true)] out Type? keyType,
            [NotNullWhen(true)] out Type? valType)
        {
            var isPair = type.GetTypeInfo().IsGenericType &&
                         type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

            if (isPair)
            {
                var genArgs = type.GetGenericArguments();
                keyType = genArgs[0];
                valType = genArgs[1];
                return true;
            }

            keyType = default;
            valType = default;
            return false;
        }

        public abstract class TypeSerializer
        {
            public abstract object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer);
            public abstract YamlNode TypeToNode(object obj, YamlObjectSerializer serializer);
        }

        /// <summary>
        ///     Basically, when you're serializing say a map file, you gotta be a liiiittle smarter than "dump all these variables to YAML".
        ///     Stuff like entity references need to handled, for example.
        ///     This can do that.
        /// </summary>
        public abstract class Context
        {
            /// <summary>
            ///     Current depth of the serialization "stack".
            ///     Basically, when another sub-serializer gets made (e.g. to handle <see cref="IExposeData" />),
            ///     This context will be passed around and this property increased to signal that.
            /// </summary>
            public int StackDepth { get; protected internal set; } = 0;

            public virtual bool TryTypeToNode(object obj, [NotNullWhen(true)] out YamlNode? node)
            {
                node = null;
                return false;
            }

            public virtual bool TryNodeToType(YamlNode node, Type type, [NotNullWhen(true)] out object? obj)
            {
                obj = default;
                return false;
            }

            public virtual bool IsValueDefault<T>(string field, T value, WithFormat<T> format)
            {
                return false;
            }

            public virtual bool TryGetCachedField<T>(string field, [MaybeNullWhen(false)] out T value)
            {
                value = default;
                return false;
            }

            public virtual void SetCachedField<T>(string field, T value)
            {
            }

            public virtual bool TryGetDataCache(string field, out object? value)
            {
                value = null;
                return false;
            }

            public virtual void SetDataCache(string field, object value)
            {
            }
        }

        class StructSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                var mapNode = (YamlMappingNode)node;

                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var instance = Activator.CreateInstance(type)!;
                var scalarNode = new YamlScalarNode();

                foreach (var field in fields)
                {
                    if (field.IsNotSerialized)
                        continue;

                    var fName = field.Name;
                    var fType = field.FieldType;

                    scalarNode.Value = fName;

                    if (mapNode.Children.TryGetValue(scalarNode, out var fNode))
                    {
                        var fVal = serializer.NodeToType(fType, fNode);
                        field.SetValue(instance, fVal);
                    }
                }

                return instance;
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var node = new YamlMappingNode();
                var type = obj.GetType();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    if (field.IsNotSerialized)
                        continue;

                    var fVal = field.GetValue(obj);

                    if (fVal == null)
                    {
                        throw new ArgumentException("Cannot serialize null value inside struct field.");
                    }

                    // Potential recursive infinite loop?
                    var fTypeNode = serializer.TypeToNode(fVal);
                    node.Add(field.Name, fTypeNode);
                }

                return node;
            }
        }

        class ColorSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                return node.AsColor();
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var color = (Color)obj;

                return new YamlScalarNode(color.ToHex());
            }
        }

        class MapIdSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                var val = int.Parse(node.ToString(), CultureInfo.InvariantCulture);
                return new MapId(val);
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var val = (int)(MapId)obj;
                return new YamlScalarNode(val.ToString());
            }
        }

        class GridIdSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                return new GridId(node.AsInt());
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var val = (int)(GridId)obj;
                return new YamlScalarNode(val.ToString());
            }
        }

        class Vector2Serializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                return node.AsVector2();
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var vec = (Vector2)obj;
                return new YamlScalarNode($"{vec.X.ToString(CultureInfo.InvariantCulture)},{vec.Y.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        class Vector3Serializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                return node.AsVector3();
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var vec = (Vector3)obj;
                return new YamlScalarNode($"{vec.X.ToString(CultureInfo.InvariantCulture)},{vec.Y.ToString(CultureInfo.InvariantCulture)},{vec.Z.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        class Vector4Serializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                return node.AsVector4();
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var vec = (Vector4)obj;
                return new YamlScalarNode($"{vec.X.ToString(CultureInfo.InvariantCulture)},{vec.Y.ToString(CultureInfo.InvariantCulture)},{vec.Z.ToString(CultureInfo.InvariantCulture)},{vec.W.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        class AngleSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                var nodeContents = node.AsString();
                if (nodeContents.EndsWith("rad"))
                {
                    return new Angle(double.Parse(nodeContents.Substring(0, nodeContents.Length - 3), CultureInfo.InvariantCulture));
                }
                return Angle.FromDegrees(double.Parse(nodeContents, CultureInfo.InvariantCulture));
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var val = ((Angle)obj).Theta;
                return new YamlScalarNode($"{val.ToString(CultureInfo.InvariantCulture)} rad");
            }
        }

        class UIBox2Serializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                var args = node.ToString().Split(',');

                var t = float.Parse(args[0], CultureInfo.InvariantCulture);
                var l = float.Parse(args[1], CultureInfo.InvariantCulture);
                var b = float.Parse(args[2], CultureInfo.InvariantCulture);
                var r = float.Parse(args[3], CultureInfo.InvariantCulture);

                return new UIBox2(l, t, r, b);
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var box = (UIBox2)obj;
                return new YamlScalarNode($"{box.Top.ToString(CultureInfo.InvariantCulture)},{box.Left.ToString(CultureInfo.InvariantCulture)},{box.Bottom.ToString(CultureInfo.InvariantCulture)},{box.Right.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        class Box2Serializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                var args = node.ToString().Split(',');

                var b = float.Parse(args[0], CultureInfo.InvariantCulture);
                var l = float.Parse(args[1], CultureInfo.InvariantCulture);
                var t = float.Parse(args[2], CultureInfo.InvariantCulture);
                var r = float.Parse(args[3], CultureInfo.InvariantCulture);

                return new Box2(l, b, r, t);
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var box = (Box2)obj;
                return new YamlScalarNode($"{box.Bottom.ToString(CultureInfo.InvariantCulture)},{box.Left.ToString(CultureInfo.InvariantCulture)},{box.Top.ToString(CultureInfo.InvariantCulture)},{box.Right.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        class ResourcePathSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                return node.AsResourcePath();
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                return new YamlScalarNode(obj.ToString());
            }
        }

        class SpriteSpecifierSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                return SpriteSpecifier.FromYaml(node);
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                switch (obj)
                {
                    case SpriteSpecifier.Texture tex:
                        return tex.TexturePath.ToString();
                    case SpriteSpecifier.Rsi rsi:
                        var mapping = new YamlMappingNode();
                        mapping.Add("sprite", rsi.RsiPath.ToString());
                        mapping.Add("state", rsi.RsiState);
                        return mapping;
                }
                throw new NotImplementedException();
            }
        }

        class TimeSpanSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                var seconds = double.Parse(node.AsString(), CultureInfo.InvariantCulture);
                return TimeSpan.FromSeconds(seconds);
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var seconds = ((TimeSpan) obj).TotalSeconds;
                return new YamlScalarNode(seconds.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
