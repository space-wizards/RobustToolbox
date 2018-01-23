using System;
using System.Globalization;
using System.IO;
using OpenTK;
using SS14.Shared.Interfaces;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Shared.GameObjects
{
    public class EntityYamlSerializer : EntitySerializer
    {
        /// <summary>
        ///     Reading or Writing to the stream.
        /// </summary>
        private bool _read;

        private readonly YamlDocument _document;
        private readonly YamlSequenceNode _root;
        private YamlMappingNode _entMap;
        private YamlSequenceNode _compSeq;

        private YamlMappingNode _curMap;

        public EntityYamlSerializer()
        {
            _root = new YamlSequenceNode();
            _document = new YamlDocument(_root);
        }

        public EntityYamlSerializer(YamlMappingNode entMap)
        {
            _read = true;
            _curMap = entMap;
        }

        /// <summary>
        ///     
        /// </summary>
        public override void EntityHeader()
        {
            if(!_read)
            {
                _entMap = new YamlMappingNode();
                _root.Children.Add(_entMap);
                _curMap = _entMap;
            }
        }

        public override void EntityFooter()
        {
            if(!_read)
            {
                _curMap = null;
            }
        }

        public override void CompHeader()
        {
            if(!_read)
            {
                _compSeq = new YamlSequenceNode();
                _entMap.Children.Add("components", _compSeq);
                _curMap = null;
            }
        }

        public override void CompStart()
        {
            if(!_read)
            {
                _curMap = new YamlMappingNode();
                _compSeq.Children.Add(_curMap);
            }
        }

        public override void CompFooter()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public override void DataField<T>(ref T value, string name, T defaultValue, bool alwaysWrite = false)
        {
            if (_read) // read
            {
                if (_curMap.TryGetNode(name, out var node))
                {
                    value = (T)NodeToType(node, typeof(T));
                }
                else
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

        public void WriteToFile(string yamlPath)
        {
            var resMan = IoCManager.Resolve<IResourceManager>();
            var rootPath = resMan.ConfigDirectory;
            var path = Path.Combine(rootPath, yamlPath);
            var fullPath = Path.GetFullPath(path);

            var dir = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(dir);

            using (var writer = new StreamWriter(fullPath))
            {
                var stream = new YamlStream();
                
                stream.Add(_document);
                
                stream.Save(writer);
            }
        }

        private static object NodeToType(YamlNode node, Type type)
        {
            if (type == typeof(Vector2))
            {
                return node.AsVector2();
            }
            else if (type == typeof(Box2))
            {
                var raw = node.AsString();
                var args = raw.Split(',');
                if (args.Length != 4)
                {
                    throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector2), raw));
                }

                var left = float.Parse(args[0], CultureInfo.InvariantCulture);
                var top = float.Parse(args[1], CultureInfo.InvariantCulture);
                var right = float.Parse(args[2], CultureInfo.InvariantCulture);
                var bottom = float.Parse(args[3], CultureInfo.InvariantCulture);
                return new Box2(left, top, right, bottom);
            }

            throw new ArgumentException($"Type {type.FullName} is not supported.", nameof(type));
        }

        private static YamlNode TypeToNode(object obj)
        {
            switch (obj)
            {
                case string val:
                    return val;
                case Vector2 val:
                    return $"{val.X},{val.Y}";
                case Box2 val:
                    return $"{val.Left},{val.Top},{val.Bottom},{val.Right}";
            }

            throw new ArgumentException($"Type {obj.GetType().FullName} is not supported.", nameof(obj));
        }
    }
}
