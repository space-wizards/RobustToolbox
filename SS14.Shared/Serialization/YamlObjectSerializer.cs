using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Serialization
{
    public class YamlObjectSerializer : ObjectSerializer
    {
        private static readonly Dictionary<Type, TypeSerializer> _typeSerializers;
        private static readonly StructSerializer _structSerializer;

        private bool SetDefaults;

        private YamlMappingNode Map;
        private List<YamlMappingNode> Backups;

        static YamlObjectSerializer()
        {
            _structSerializer = new StructSerializer();
            _typeSerializers = new Dictionary<Type, TypeSerializer>
            {
                { typeof(Color), new ColorSerializer() },
                { typeof(MapId), new MapIdSerializer() },
                { typeof(GridId), new GridIdSerializer() },
                { typeof(Vector2), new Vector2Serializer() },
                { typeof(Angle), new AngleSerializer() },
                { typeof(Box2), new Box2Serializer() }
            };
        }

        public YamlObjectSerializer(YamlMappingNode map, bool reading, bool setDefaults = true, List<YamlMappingNode> backups = null)
        {
            Map = map;
            Reading = reading;
            SetDefaults = setDefaults;
            Backups = backups;
        }

        public override void DataField<T>(ref T value, string name, T defaultValue, bool alwaysWrite = false)
        {
            if (Reading) // read
            {
                if (Map.TryGetNode(name, out var node))
                {
                    value = (T)NodeToType(typeof(T), node);
                }
                else if (SetDefaults)
                {
                    value = defaultValue;
                }
            }
            else // write
            {
                // don't write if value is null or default
                if (!alwaysWrite && (value != null || defaultValue == null) && (value == null || value.Equals(defaultValue)))
                    return;

                var key = name;
                var val = value == null ? TypeToNode(defaultValue) : TypeToNode(value);
                Map.Add(key, val);
            }
        }

        public override void DataReadFunction<T>(string name, T defaultValue, ReadFunctionDelegate<T> func)
        {
            if (!Reading) return;

            if (Map.TryGetNode(name, out var node))
            {
                var value = (T)NodeToType(typeof(T), node);
                func.Invoke(value);
            }
            else if (SetDefaults)
            {
                var value = defaultValue;
                func.Invoke(value);
            }
        }

        public override void DataWriteFunction<T>(string name, T defaultValue, WriteFunctionDelegate<T> func, bool alwaysWrite = false)
        {
            if (Reading) return;

            var value = func.Invoke();

            // don't write if value is null or default
            if (!alwaysWrite && (value != null || defaultValue == null) && (value == null || value.Equals(defaultValue)))
                return;

            var key = name;
            var val = value == null ? TypeToNode(defaultValue) : TypeToNode(value);
            Map.Add(key, val);
        }

        public static object NodeToType(Type type, YamlNode node)
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

            // custom TypeSerializer
            if (_typeSerializers.TryGetValue(type, out var serializer))
                return serializer.NodeToType(type, node);

            // other val (struct)
            if (type.IsValueType)
                return _structSerializer.NodeToType(type, (YamlMappingNode)node);

            // ref type that isn't a custom TypeSerializer
            throw new ArgumentException($"Type {type.FullName} is not supported.", nameof(type));
        }

        public static YamlNode TypeToNode(object obj)
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

            // custom TypeSerializer
            if (_typeSerializers.TryGetValue(type, out var serializer))
                return serializer.TypeToNode(obj);

            // other val (struct)
            if (type.IsValueType)
                return _structSerializer.TypeToNode(obj);

            // ref type that isn't a custom TypeSerializer
            throw new ArgumentException($"Type {type.FullName} is not supported.", nameof(obj));
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
            public abstract object NodeToType(Type type, YamlNode node);
            public abstract YamlNode TypeToNode(object obj);
        }

        class StructSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node)
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
                        var fVal = YamlObjectSerializer.NodeToType(fType, fNode);
                        field.SetValue(instance, fVal);
                    }
                }

                return instance;
            }

            public override YamlNode TypeToNode(object obj)
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
                    var fTypeNode = YamlObjectSerializer.TypeToNode(fVal);
                    node.Add(field.Name, fTypeNode);
                }

                return node;
            }
        }

        class ColorSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node)
            {
                return node.AsColor();
            }

            public override YamlNode TypeToNode(object obj)
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
            public override object NodeToType(Type type, YamlNode node)
            {
                var val = int.Parse(node.ToString());
                return new MapId(val);
            }

            public override YamlNode TypeToNode(object obj)
            {
                var val = (int)(MapId)obj;
                return new YamlScalarNode(val.ToString());
            }
        }

        class GridIdSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node)
            {
                return new GridId(node.AsInt());
            }

            public override YamlNode TypeToNode(object obj)
            {
                var val = (int)(GridId)obj;
                return new YamlScalarNode(val.ToString());
            }
        }

        class Vector2Serializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node)
            {
                return node.AsVector2();
            }

            public override YamlNode TypeToNode(object obj)
            {
                var vec = (Vector2)obj;
                return new YamlScalarNode($"{vec.X},{vec.Y}");
            }
        }

        class AngleSerializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node)
            {
                var val = float.Parse(node.ToString());
                return new Angle(val);
            }

            public override YamlNode TypeToNode(object obj)
            {
                var val = (float)((Angle)obj).Theta;
                return new YamlScalarNode(val.ToString(CultureInfo.InvariantCulture));
            }
        }

        class Box2Serializer : TypeSerializer
        {
            public override object NodeToType(Type type, YamlNode node)
            {
                var args = node.ToString().Split(',');

                var t = float.Parse(args[0], CultureInfo.InvariantCulture);
                var l = float.Parse(args[1], CultureInfo.InvariantCulture);
                var b = float.Parse(args[2], CultureInfo.InvariantCulture);
                var r = float.Parse(args[3], CultureInfo.InvariantCulture);

                return new Box2(l, t, r, b);
            }

            public override YamlNode TypeToNode(object obj)
            {
                var box = (Box2)obj;
                return new YamlScalarNode($"{box.Top},{box.Left},{box.Bottom},{box.Right}");
            }
        }
    }
}
