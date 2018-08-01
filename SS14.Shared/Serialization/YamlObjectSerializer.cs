using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Serialization
{
    /// <summary>
    ///     Object serializer that serializes to/from YAML.
    /// </summary>
    public class YamlObjectSerializer : ObjectSerializer
    {
        private static readonly Dictionary<Type, TypeSerializer> _typeSerializers;
        private static readonly StructSerializer _structSerializer;

        private YamlMappingNode WriteMap;
        private List<YamlMappingNode> ReadMaps;
        private Context _context;

        static YamlObjectSerializer()
        {
            _structSerializer = new StructSerializer();
            _typeSerializers = new Dictionary<Type, TypeSerializer>
            {
                { typeof(Color), new ColorSerializer() },
                { typeof(Vector2), new Vector2Serializer() },
                { typeof(Angle), new AngleSerializer() },
                { typeof(Box2), new Box2Serializer() },
                { typeof(ResourcePath), new ResourcePathSerializer() },
                { typeof(GridId), new GridIdSerializer() },
                { typeof(MapId), new MapIdSerializer() },
                { typeof(SpriteSpecifier), new SpriteSpecifierSerializer() },
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
        public static YamlObjectSerializer NewReader(YamlMappingNode readMap, Context context = null)
        {
            return NewReader(new List<YamlMappingNode>(1) { readMap });
        }

        /// <summary>
        ///     Creates a new serializer to be used for reading from YAML data.
        /// </summary>
        /// <param name="readMaps">
        ///     A list of maps to read from. The first list will be used first,
        ///     then the second if the first does not contain a specific key, and so on.
        /// </param>
        /// <param name="context">
        ///     An optional context that can provide additional capabitilies such as caching and custom type serializers.
        /// </param>
        public static YamlObjectSerializer NewReader(List<YamlMappingNode> readMaps, Context context = null)
        {
            return new YamlObjectSerializer
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
        public static YamlObjectSerializer NewWriter(YamlMappingNode writeMap, Context context = null)
        {
            return new YamlObjectSerializer
            {
                WriteMap = writeMap,
                _context = context,
                Reading = false,
            };
        }

        // TODO: Theoretical optimization.
        // Might be a good idea to make DataField<T> use caching for value types without references too.
        /// <inheritdoc />
        public override void DataField<T>(ref T value, string name, T defaultValue, bool alwaysWrite = false)
        {
            if (Reading) // read
            {
                foreach (var map in ReadMaps)
                {
                    if (map.TryGetNode(name, out var node))
                    {
                        value = (T)NodeToType(typeof(T), node);
                        return;
                    }
                }
                value = defaultValue;
                return;
            }
            else // write
            {
                // don't write if value is null or default
                if (!alwaysWrite && IsValueDefault(name, value, defaultValue))
                    return;

                var key = name;
                var val = value == null ? TypeToNode(defaultValue) : TypeToNode(value);
                WriteMap.Add(key, val);
            }
        }

        /// <inheritdoc />
        public override void DataFieldCached<T>(ref T value, string name, T defaultValue, bool alwaysWrite = false)
        {
            if (Reading) // read
            {
                if (_context != null && _context.TryGetCachedField(name, out T theValue))
                {
                    // Itermediate field so value doesn't get reset to default(T) if this fails.
                    value = theValue;
                    return;
                }
                foreach (var map in ReadMaps)
                {
                    if (map.TryGetNode(name, out var node))
                    {
                        value = (T)NodeToType(typeof(T), node);
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
                DataField(ref value, name, defaultValue, alwaysWrite);
            }
        }

        /// <inheritdoc />
        public override void DataField<TTarget, TSource>(
            ref TTarget value,
            string name,
            TTarget defaultValue,
            Func<TSource, TTarget> ReadConvertFunc,
            Func<TTarget, TSource> WriteConvertFunc = null,
            bool alwaysWrite = false)
        {
            if (Reading)
            {
                foreach (var map in ReadMaps)
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
                if (!alwaysWrite && IsValueDefault(name, value, defaultValue))
                {
                    return;
                }

                var key = name;
                var val = value == null ? TypeToNode(WriteConvertFunc(defaultValue)) : TypeToNode(WriteConvertFunc(value));
                WriteMap.Add(key, val);
            }
        }

        /// <inheritdoc />
        public override void DataFieldCached<TTarget, TSource>(
            ref TTarget value,
            string name,
            TTarget defaultValue,
            Func<TSource, TTarget> ReadConvertFunc,
            Func<TTarget, TSource> WriteConvertFunc = null,
            bool alwaysWrite = false)
        {
            if (Reading)
            {
                if (_context != null && _context.TryGetCachedField(name, out TTarget theValue))
                {
                    // Itermediate field so value doesn't get reset to default(T) if this fails.
                    value = theValue;
                    return;
                }
                foreach (var map in ReadMaps)
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

            foreach (var map in ReadMaps)
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

            if (_context != null && _context.TryGetCachedField(name, out T val))
            {
                return val;
            }

            foreach (var map in ReadMaps)
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
        public override bool TryReadDataField<T>(string name, out T value)
        {
            if (!Reading)
            {
                throw new InvalidOperationException("Cannot use ReadDataField while not reading.");
            }

            foreach (var map in ReadMaps)
            {
                if (map.TryGetNode(name, out var node))
                {
                    value = (T)NodeToType(typeof(T), node);
                    return true;
                }
            }
            value = default(T);
            return false;
        }

        /// <inheritdoc />
        public override bool TryReadDataFieldCached<T>(string name, out T value)
        {
            if (!Reading)
            {
                throw new InvalidOperationException("Cannot use ReadDataField while not reading.");
            }

            if (_context != null && _context.TryGetCachedField(name, out value))
            {
                return true;
            }

            foreach (var map in ReadMaps)
            {
                if (map.TryGetNode(name, out var node))
                {
                    value = (T)NodeToType(typeof(T), node);
                    _context?.SetCachedField(name, value);
                    return true;
                }
            }
            value = default(T);
            return false;
        }

        /// <inheritdoc />
        public override void DataReadFunction<T>(string name, T defaultValue, ReadFunctionDelegate<T> func)
        {
            if (!Reading) return;

            foreach (var map in ReadMaps)
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
            if (!alwaysWrite && IsValueDefault(name, value, defaultValue))
                return;

            var key = name;
            var val = value == null ? TypeToNode(defaultValue) : TypeToNode(value);
            WriteMap.Add(key, val);
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
                return (T)value;
            }
            throw new KeyNotFoundException();
        }

        /// <inheritdoc />
        public override bool TryGetCacheData<T>(string key, out T data)
        {
            if (_context != null && _context.TryGetDataCache(key, out var value))
            {
                data = (T)value;
                return true;
            }

            data = default(T);
            return false;
        }

        public object NodeToType(Type type, YamlNode node)
        {
            // special snowflake string
            if (type == typeof(String))
                return node.ToString();

            // val primitives
            if (type.IsPrimitive)
                return StringToType(type, node.ToString());

            // val enum
            if (type.IsEnum)
                return Enum.Parse(type, node.ToString());

            // List<T>
            if (TryGenericListType(type, out var listType))
            {
                var listNode = (YamlSequenceNode)node;
                var newList = (IList)Activator.CreateInstance(type);

                foreach (var entryNode in listNode)
                {
                    var value = NodeToType(listType, entryNode);
                    newList.Add(value);
                }

                return newList;
            }

            // Dictionary<K,V>
            if (TryGenericDictType(type, out var keyType, out var valType))
            {
                var dictNode = (YamlMappingNode)node;
                var newDict = (IDictionary)Activator.CreateInstance(type);

                foreach (var kvEntry in dictNode.Children)
                {
                    var keyValue = NodeToType(keyType, kvEntry.Key);
                    var valValue = NodeToType(valType, kvEntry.Value);

                    newDict.Add(keyValue, valValue);
                }

                return newDict;
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
                    throw new InvalidOperationException("Cannot read from IExposeData on non-mapping node.");
                }
                var instance = (IExposeData)Activator.CreateInstance(type);
                // TODO: Might be worth it to cut down on allocations here by using ourselves instead of creating a fork.
                // Seems doable.
                if (_context != null)
                {
                    _context.StackDepth++;
                }
                var fork = NewReader(mapNode, _context);
                if (_context != null)
                {
                    _context.StackDepth--;
                }
                instance.ExposeData(fork);
                return instance;
            }

            // other val (struct)
            if (type.IsValueType)
                return _structSerializer.NodeToType(type, (YamlMappingNode)node, this);

            // ref type that isn't a custom TypeSerializer
            throw new ArgumentException($"Type {type.FullName} is not supported.", nameof(type));
        }

        public YamlNode TypeToNode(object obj)
        {
            // special snowflake string
            if (obj is string s)
                return s;

            var type = obj.GetType();

            // val primitives and val enums
            if (type.IsPrimitive || type == typeof(Enum))
                return obj.ToString();

            // List<T>
            if (TryGenericListType(type, out var listType))
            {
                var node = new YamlSequenceNode();

                foreach (var entry in (IEnumerable)obj)
                {
                    var entryNode = TypeToNode(entry);
                    node.Add(entryNode);
                }

                return node;
            }

            // Dictionary<K,V>
            if (TryGenericDictType(type, out var keyType, out var valType))
            {
                var node = new YamlMappingNode();

                foreach (DictionaryEntry entry in (IDictionary)obj)
                {
                    var keyNode = TypeToNode(entry.Key);
                    var valNode = TypeToNode(entry.Value);

                    node.Add(keyNode, valNode);
                }

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
                if (_context != null)
                {
                    _context.StackDepth--;
                }
                exposable.ExposeData(fork);
                return mapping;
            }

            // other val (struct)
            if (type.IsValueType)
                return _structSerializer.TypeToNode(obj, this);

            // ref type that isn't a custom TypeSerializer
            throw new ArgumentException($"Type {type.FullName} is not supported.", nameof(obj));
        }

        bool IsValueDefault<T>(string field, T value, T providedDefault)
        {
            if ((value != null || providedDefault == null) && (value == null || value.Equals(providedDefault)))
            {
                return true;
            }

            if (_context != null)
            {
                return _context.IsValueDefault(field, value);
            }

            return false;

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

        private static bool TryGenericListType(Type type, out Type listType)
        {
            var isList = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

            if (isList)
            {
                listType = type.GetGenericArguments()[0];
                return true;
            }

            listType = default(Type);
            return false;
        }

        private static bool TryGenericDictType(Type type, out Type keyType, out Type valType)
        {
            var isDict = type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

            if (isDict)
            {
                var genArgs = type.GetGenericArguments();
                keyType = genArgs[0];
                valType = genArgs[1];
                return true;
            }

            keyType = default(Type);
            valType = default(Type);
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

            public virtual bool TryTypeToNode(object obj, out YamlNode node)
            {
                node = null;
                return false;
            }

            public virtual bool TryNodeToType(YamlNode node, Type type, out object obj)
            {
                obj = default(object);
                return false;
            }

            public virtual bool IsValueDefault<T>(string field, T value)
            {
                return false;
            }

            public virtual bool TryGetCachedField<T>(string field, out T value)
            {
                value = default(T);
                return false;
            }

            public virtual void SetCachedField<T>(string field, T value)
            {
            }

            public virtual bool TryGetDataCache(string field, out object value)
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
                var mapNode = node as YamlMappingNode;

                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var instance = Activator.CreateInstance(type);
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

                Int32 hexColor = 0;
                hexColor += color.RByte << 24;
                hexColor += color.GByte << 16;
                hexColor += color.BByte << 8;
                hexColor += color.AByte;

                return new YamlScalarNode("#" + hexColor.ToString("X"));
            }
        }

        class MapIdSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                var val = int.Parse(node.ToString());
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
                return new YamlScalarNode($"{vec.X},{vec.Y}");
            }
        }

        class AngleSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                var val = float.Parse(node.ToString());
                return new Angle(val);
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var val = (float)((Angle)obj).Theta;
                return new YamlScalarNode(val.ToString(CultureInfo.InvariantCulture));
            }
        }

        class Box2Serializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
            {
                var args = node.ToString().Split(',');

                var t = float.Parse(args[0], CultureInfo.InvariantCulture);
                var l = float.Parse(args[1], CultureInfo.InvariantCulture);
                var b = float.Parse(args[2], CultureInfo.InvariantCulture);
                var r = float.Parse(args[3], CultureInfo.InvariantCulture);

                return new Box2(l, t, r, b);
            }

            public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
            {
                var box = (Box2)obj;
                return new YamlScalarNode($"{box.Top},{box.Left},{box.Bottom},{box.Right}");
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
                var specifier = (SpriteSpecifier)obj;
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
    }
}
