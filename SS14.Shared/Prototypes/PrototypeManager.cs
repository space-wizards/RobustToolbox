using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;
using SS14.Shared.Utility;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SS14.Shared.Interfaces;
using SS14.Shared.Log;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using OpenTK;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.GameObjects;

namespace SS14.Shared.Prototypes
{
    /// <summary>
    /// Handle storage and loading of YAML prototypes.
    /// </summary>
    public interface IPrototypeManager
    {
        /// <summary>
        /// Return an IEnumerable to iterate all prototypes of a certain type.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the type of prototype is not registered.
        /// </exception>
        IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype;
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
        T Index<T>(string id) where T : class, IIndexedPrototype;
        /// <summary>
        /// Index for a <see cref="IIndexedPrototype"/> by ID.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the ID does not exist or the type of prototype is not registered.
        /// </exception>
        IIndexedPrototype Index(Type type, string id);
        bool HasIndex<T>(string id) where T : IIndexedPrototype;
        bool TryIndex<T>(string id, out T prototype) where T : IIndexedPrototype;
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
        private readonly string type;
        public string Type => type;
        public PrototypeAttribute(string type)
        {
            this.type = type;
        }
    }

    public class PrototypeManager : IPrototypeManager, IPostInjectInit
    {
        [Dependency]
        private readonly IReflectionManager ReflectionManager;
        private readonly Dictionary<string, Type> prototypeTypes = new Dictionary<string, Type>();

        [Dependency]
        private readonly IResourceManager _resources;

        #region IPrototypeManager members
        private readonly Dictionary<Type, List<IPrototype>> prototypes = new Dictionary<Type, List<IPrototype>>();
        private readonly Dictionary<Type, Dictionary<string, IIndexedPrototype>> indexedPrototypes = new Dictionary<Type, Dictionary<string, IIndexedPrototype>>();

        public IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype
        {
            return prototypes[typeof(T)].Select((IPrototype p) => (T)p);
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(Type type)
        {
            return prototypes[type];
        }

        public T Index<T>(string id) where T : class, IIndexedPrototype
        {
            try
            {
                return (T)indexedPrototypes[typeof(T)][id];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownPrototypeException(id);
            }
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
            foreach (Type type in prototypeTypes.Values.Where(t => typeof(ISyncingPrototype).IsAssignableFrom(t)))
            {
                // This list is the list of prototypes we're syncing.
                // Iterate using indices.
                // IF the prototype wants to NOT by synced again,
                // Swap remove it with the one at the end of the list,
                //  and do the whole thing again with the one formerly at the end of the list
                // otherwise keep it and move up an index
                // When we get to the end, do the whole thing again!
                // Yes this is ridiculously overengineered BUT IT PERFORMS WELL.
                // I hope.
                List<ISyncingPrototype> currentRun = prototypes[type].Select(p => (ISyncingPrototype)p).ToList();
                int stage = 0;
                // Outer loop to iterate stages.
                while (currentRun.Count > 0)
                {
                    // Increase positions to iterate over list.
                    // If we need to stick, i gets reduced down below.
                    for (int i = 0; i < currentRun.Count; i++)
                    {
                        ISyncingPrototype prototype = currentRun[i];
                        bool result = prototype.Sync(this, stage);
                        // Keep prototype and move on to next one if it returns true.
                        // Thus it stays in the list for next stage.
                        if (result)
                        {
                            continue;
                        }

                        // Move the last element in the list to where we are currently.
                        // Since we don't break we'll do this one next, as i stays the same.
                        //  (for loop cancels out decrement here)
                        currentRun.RemoveSwap(i);
                        i--;
                    }
                    stage++;
                }
            }
        }

        /// <inheritdoc />
        public void LoadDirectory(string path)
        {
            foreach (var filePath in _resources.ContentFindFiles(path))
            {
                using (var reader = new StreamReader(_resources.ContentFileRead(filePath), Encoding.UTF8))
                {
                    try
                    {
                        LoadFromStream(reader);
                    }
                    catch (Exception e)
                        when (e is YamlException || e is PrototypeLoadException)
                    {
                        Logger.Error($"[ENG] Exception whilst loading prototypes from {filePath}: {e}");
                    }
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

        public void PostInject()
        {
            ReflectionManager.OnAssemblyAdded += (_, __) => ReloadPrototypeTypes();
            ReloadPrototypeTypes();
        }

        private void ReloadPrototypeTypes()
        {
            Clear();
            foreach (var type in ReflectionManager.GetAllChildren<IPrototype>())
            {
                var attribute = (PrototypeAttribute)Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute));
                if (attribute == null)
                {
                    throw new InvalidImplementationException(type, typeof(IPrototype), "No " + nameof(PrototypeAttribute) + " to give it a type string.");
                }

                if (prototypeTypes.ContainsKey(attribute.Type))
                {
                    throw new InvalidImplementationException(type, typeof(IPrototype), string.Format("Duplicate prototype type ID: {0}. Current: {1}", attribute.Type, prototypeTypes[attribute.Type]));
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
                var indexedPrototype = prototype as IIndexedPrototype;
                if (indexedPrototype != null)
                {
                    var id = indexedPrototype.ID;
                    if (indexedPrototypes[prototypeType].ContainsKey(id))
                    {
                        throw new PrototypeLoadException(string.Format("Duplicate ID: '{0}'", id));
                    }
                    indexedPrototypes[prototypeType][id] = (IIndexedPrototype)prototype;
                }
            }
        }

        public bool HasIndex<T>(string id) where T : IIndexedPrototype
        {
            if (!indexedPrototypes.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }
            return index.ContainsKey(id);
        }

        public bool TryIndex<T>(string id, out T prototype) where T : IIndexedPrototype
        {
            if (!indexedPrototypes.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }
            var returned = index.TryGetValue(id, out var uncast);
            prototype = (T)uncast;
            return returned;
        }
    }

    [Serializable]
    public class PrototypeLoadException : Exception
    {
        public PrototypeLoadException()
        {
        }
        public PrototypeLoadException(string message) : base(message)
        {
        }
        public PrototypeLoadException(string message, Exception inner) : base(message, inner)
        {
        }

        public PrototypeLoadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }

    [Serializable]
    public class UnknownPrototypeException : Exception
    {
        public override string Message => "Unknown prototype: " + Prototype;
        public readonly string Prototype;
        public UnknownPrototypeException(string prototype)
        {
            Prototype = prototype;
        }

        public UnknownPrototypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Prototype = (string)info.GetValue("prototype", typeof(string));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("prototype", Prototype, typeof(string));
        }
    }
}
