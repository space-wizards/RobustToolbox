using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Prototypes
{
    /// <summary>
    /// Handle storage and loading of YAML prototypes.
    /// </summary>
    public interface IPrototypeManager : IIoCInterface
    {
        /// <summary>
        /// Return an IEnumerable to iterate all prototypes of a certain type.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the type of prototype is not registered.
        /// </exception>
        IEnumerable<T> EnumeratePrototypes<T>() where T: class, IPrototype;
        /// <summary>
        /// Return an IEnumerable to iterate all prototypes of a certain type.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the type of prototype is not registered.
        /// </exception>
        IEnumerable<IPrototype> EnumeratePrototypes(Type type);
        /// <summary>
        /// Index for a <see cref="IIndexedPrototype"/> by ID.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the type of prototype is not registered.
        /// </exception>
        T Index<T>(string id) where T: class, IIndexedPrototype;
        /// <summary>
        /// Index for a <see cref="IIndexedPrototype"/> by ID.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the ID does not exist or the type of prototype is not registered.
        /// </exception>
        IIndexedPrototype Index(Type type, string id);
        /// <summary>
        /// Load prototypes from files in a directory, recursively.
        /// </summary>
        void LoadDirectory(string path);
        void LoadFromStream(TextReader stream);
        /// <summary>
        /// Clear out all prototypes and reset to a blank slate.
        /// </summary>
        void Clear();
        /// <summary>
        /// Syncs all inter-prototype data. Call this when operations adding new prototypes are done.
        /// </summary>
        void Resync();
    }

    /// <summary>
    /// Quick attribute to give the prototype its type string.
    /// To prevent needing to instantiate it because interfaces can't declare statics.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class PrototypeAttribute : Attribute
    {
        private string type;
        public string Type => type;
        public PrototypeAttribute(string type)
        {
            this.type = type;
        }
    }

    [IoCTarget]
    public class PrototypeManager : IPrototypeManager
    {
        private Dictionary<string, Type> prototypeTypes = new Dictionary<string, Type>();

        #region IPrototypeManager members
        private Dictionary<Type, List<IPrototype>> prototypes = new Dictionary<Type, List<IPrototype>>();
        private Dictionary<Type, Dictionary<string, IIndexedPrototype>> indexedPrototypes = new Dictionary<Type, Dictionary<string, IIndexedPrototype>>();

        public IEnumerable<T> EnumeratePrototypes<T>() where T: class, IPrototype
        {
            return prototypes[typeof(T)].Select((IPrototype p) => p as T);
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(Type type)
        {
            return prototypes[type];
        }

        public T Index<T>(string id) where T: class, IIndexedPrototype
        {
            return indexedPrototypes[typeof(T)][id] as T;
        }

        public IIndexedPrototype Index(Type type, string id)
        {
            return indexedPrototypes[type][id];
        }

        public void Clear()
        {
            prototypes.Clear();
            prototypeTypes.Clear();
            indexedPrototypes.Clear();
        }

        public void Resync()
        {
            foreach (Type type in prototypeTypes.Values.Where((Type type) => typeof(ISyncingPrototype).IsAssignableFrom(type)))
            {
                foreach (ISyncingPrototype prototype in prototypes[type].Select((IPrototype p) => p as ISyncingPrototype))
                {
                    prototype.Sync(this);
                }
            }
        }

        public void LoadDirectory(string path)
        {
            foreach (string filePath in PathHelpers.GetFiles(path))
            {
                var file = File.OpenRead(filePath);
                var stream = new StreamReader(file, Encoding.UTF8);

                try
                {
                    LoadFromStream(stream);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception whilst loading prototypes from {0}: {1}", filePath, e);
                }
            }
        }

        public void LoadFromStream(TextReader stream)
        {
            var yaml = new YamlStream();
            yaml.Load(stream);

            for (int i = 0; i < yaml.Documents.Count; i++)
            {
                try
                {
                    LoadFromDocument(yaml.Documents[i]);
                }
                catch (Exception e)
                {
                    throw new PrototypeLoadException(string.Format("Failed to load prototypes from document#{0}", i), e);
                }
            }
        }

        #endregion IPrototypeManager members

        public PrototypeManager()
        {
            IoCManager.AssemblyAdded += ReloadPrototypeTypes;
            ReloadPrototypeTypes();
        }

        private void ReloadPrototypeTypes()
        {
            foreach (var type in IoCManager.ResolveEnumerable<IPrototype>())
            {
                var attribute = (PrototypeAttribute)Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute));
                if (attribute == null)
                {
                    throw new Exception(string.Format("IPrototype implementor {0} does not have a PrototypeAttribute to give it a type.", type));
                }

                if (prototypeTypes.ContainsKey(attribute.Type))
                {
                    throw new Exception(string.Format("Duplicate prototype type ID on {0}: {1}. Current: {2}", type, attribute.Type, prototypeTypes[attribute.Type]));
                }

                prototypeTypes[attribute.Type] = type;
                prototypes[type] = new List<IPrototype>();
                if (typeof(IIndexedPrototype).IsAssignableFrom(type))
                {
                    indexedPrototypes[type] = new Dictionary<string, IIndexedPrototype>();
                }
            }
        }

        private void LoadFromDocument(YamlDocument document)
        {
            var rootNode = (YamlSequenceNode)document.RootNode;
            foreach (YamlMappingNode node in rootNode.Select((YamlNode n) => (YamlMappingNode)n))
            {
                var type = ((YamlScalarNode)node[new YamlScalarNode("type")]).Value;
                if (!prototypeTypes.ContainsKey(type))
                {
                    throw new PrototypeLoadException(string.Format("Unknown prototype type: '{0}'", type));
                }

                var prototypeType = prototypeTypes[type];
                var prototype = (IPrototype)Activator.CreateInstance(prototypeType);
                prototype.LoadFrom(node);
                prototypes[prototypeType].Add(prototype);
                if (prototype is IIndexedPrototype)
                {
                    var id = ((IIndexedPrototype)prototype).ID;
                    if (indexedPrototypes[prototypeType].ContainsKey(id))
                    {
                        throw new PrototypeLoadException(string.Format("Duplicate ID: '{0}'", id));
                    }
                    indexedPrototypes[prototypeType][id] = (IIndexedPrototype)prototype;
                }
            }
        }
    }

    public class PrototypeLoadException : Exception
    {
        public PrototypeLoadException() {}
        public PrototypeLoadException(string message) : base(message) {}
        public PrototypeLoadException(string message, Exception inner) : base(message, inner) {}
    }
}
