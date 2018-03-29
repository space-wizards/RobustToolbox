using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.GameObjects.Serialization
{
    public class YamlEntitySerializer : EntitySerializer
    {
        private static readonly Dictionary<Type, TypeSerializer> _typeSerializers;
        private static readonly StructSerializer _structSerializer;

        private bool _setDefaults;

        private readonly YamlSequenceNode _root;
        private YamlMappingNode _entMap;
        private YamlSequenceNode _compSeq;
        private YamlMappingNode _curMap;

        static YamlEntitySerializer()
        {
            _structSerializer = new StructSerializer();
            _typeSerializers = new Dictionary<Type, TypeSerializer>();

            _typeSerializers.Add(typeof(Color), new ColorSerializer());
            _typeSerializers.Add(typeof(MapId), new MapIdSerializer());
            _typeSerializers.Add(typeof(GridId), new GridIdSerializer());
            _typeSerializers.Add(typeof(Vector2), new Vector2Serializer());
            _typeSerializers.Add(typeof(Angle), new AngleSerializer());
            _typeSerializers.Add(typeof(Box2), new Box2Serializer());
        }

        public YamlEntitySerializer()
        {
            _root = new YamlSequenceNode();
        }

        public YamlEntitySerializer(YamlMappingNode entMap, bool setDefaults = true)
        {
            Reading = true;
            _curMap = entMap;
            _setDefaults = setDefaults;
        }

        public override void EntityHeader()
        {
            if (!Reading)
            {
                _entMap = new YamlMappingNode();
                _root.Children.Add(_entMap);
                _curMap = _entMap;
            }
        }

        public override void EntityFooter()
        {
        }

        public override void CompHeader()
        {
            if (Reading)
            {
                _compSeq = (YamlSequenceNode)_curMap.Children["components"];
                _curMap = null;
            }
            else
            {
                _compSeq = new YamlSequenceNode();
                _entMap.Children.Add("components", _compSeq);
                _curMap = null;
            }
        }

        public override void CompStart(string name)
        {
            if (Reading)
            {
                var compMaps = _compSeq.Children;

                _curMap = (YamlMappingNode)compMaps.First(m => m["type"].ToString() == name);
            }
            else
            {
                _curMap = new YamlMappingNode();
                _compSeq.Children.Add(_curMap);
            }
        }

        public override void CompFooter()
        {

        }

        public override void DataField<T>(ref T value, string name, T defaultValue, bool alwaysWrite = false)
        {
            if (Reading) // read
            {
                if (_curMap.TryGetNode(name, out var node))
                {
                    value = (T)NodeToType(typeof(T), node);
                }
                else if (_setDefaults)
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
                _curMap.Add(key, val);
            }
        }

        public override void DataSetFunction<T>(string name, T defaultValue, SetFunctionDelegate<T> func)
        {
            if (!Reading) return;

            if (_curMap.TryGetNode(name, out var node))
            {
                var value = (T)NodeToType(typeof(T), node);
                func.Invoke(value);
            }
            else if (_setDefaults)
            {
                var value = defaultValue;
                func.Invoke(value);
            }
        }

        public override void DataGetFunction<T>(string name, T defaultValue, GetFunctionDelegate<T> func, bool alwaysWrite = false)
        {
            if (Reading) return;

            var value = func.Invoke();

            // don't write if value is null or default
            if (!alwaysWrite && (value != null || defaultValue == null) && (value == null || value.Equals(defaultValue)))
                return;

            var key = name;
            var val = value == null ? TypeToNode(defaultValue) : TypeToNode(value);
            _curMap.Add(key, val);
        }

        public YamlMappingNode GetRootNode()
        {
            var root = new YamlMappingNode();
            root.Add("entities", _root);
            return root;
        }

        internal static object NodeToType(Type type, YamlNode node)
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
                var listNode = (YamlSequenceNode) node;
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
                var newDict = (IDictionary) Activator.CreateInstance(type);

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

        internal static YamlNode TypeToNode(object obj)
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

        internal static void RegisterTypeSerializer(Type type, TypeSerializer serializer)
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
    }

    internal abstract class TypeSerializer
    {
        public abstract object NodeToType(Type type, YamlNode node);
        public abstract YamlNode TypeToNode(object obj);
    }

    internal class StructSerializer : TypeSerializer
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
                    var fVal = YamlEntitySerializer.NodeToType(fType, fNode);
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
                var fTypeNode = YamlEntitySerializer.TypeToNode(fVal);
                node.Add(field.Name, fTypeNode);
            }

            return node;
        }
    }

    internal class ColorSerializer : TypeSerializer
    {
        public override object NodeToType(Type type, YamlNode node)
        {
            return Color.FromHex(node.AsString());
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

    internal class MapIdSerializer : TypeSerializer
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

    internal class GridIdSerializer : TypeSerializer
    {
        public override object NodeToType(Type type, YamlNode node)
        {
            var val = int.Parse(node.ToString());
            return new GridId(val);
        }

        public override YamlNode TypeToNode(object obj)
        {
            var val = (int)(GridId)obj;
            return new YamlScalarNode(val.ToString());
        }
    }

    internal class Vector2Serializer : TypeSerializer
    {
        public override object NodeToType(Type type, YamlNode node)
        {
            var args = node.ToString().Split(',');

            var x = float.Parse(args[0], CultureInfo.InvariantCulture);
            var y = float.Parse(args[1], CultureInfo.InvariantCulture);

            return new Vector2(x, y);
        }

        public override YamlNode TypeToNode(object obj)
        {
            var vec = (Vector2)obj;
            return new YamlScalarNode($"{vec.X},{vec.Y}");
        }
    }

    internal class AngleSerializer : TypeSerializer
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

    internal class Box2Serializer : TypeSerializer
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
