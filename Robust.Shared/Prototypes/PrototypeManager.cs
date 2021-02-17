using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.YAML;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes
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
        void LoadDirectory(ResourcePath path);
        void LoadFromStream(TextReader stream);
        /// <summary>
        /// Clear out all prototypes and reset to a blank slate.
        /// </summary>
        void Clear();
        /// <summary>
        /// Syncs all inter-prototype data. Call this when operations adding new prototypes are done.
        /// </summary>
        void Resync();

        /// <summary>
        ///     Registers a specific prototype name to be ignored.
        /// </summary>
        void RegisterIgnore(string name);

        /// <summary>
        /// Loads a single prototype class type into the manager.
        /// </summary>
        /// <param name="protoClass">A prototype class type that implements IPrototype. This type also
        /// requires a <see cref="PrototypeAttribute"/> with a non-empty class string.</param>
        void RegisterType(Type protoClass);

        event Action<YamlStream, string>? LoadedData;

    }

    /// <summary>
    /// Quick attribute to give the prototype its type string.
    /// To prevent needing to instantiate it because interfaces can't declare statics.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [BaseTypeRequired(typeof(IPrototype))]
    [MeansImplicitUse]
    [MeansDataDefinition]
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
        [Dependency] private readonly IReflectionManager ReflectionManager = default!;
        [Dependency] private readonly IDynamicTypeFactoryInternal _dynamicTypeFactory = default!;
        [Dependency] private readonly IResourceManager _resources = default!;
        [Dependency] private readonly IServ3Manager _serv3Manager = default!;

        private readonly Dictionary<string, Type> prototypeTypes = new();

        private bool _hasEverBeenReloaded;

        #region IPrototypeManager members
        private readonly Dictionary<Type, List<IPrototype>> prototypes = new();
        private readonly Dictionary<Type, Dictionary<string, IIndexedPrototype>> indexedPrototypes = new();

        private readonly HashSet<string> IgnoredPrototypeTypes = new();

        public IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }
            return prototypes[typeof(T)].Select((IPrototype p) => (T)p);
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(Type type)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }
            return prototypes[type];
        }

        public T Index<T>(string id) where T : class, IIndexedPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }
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
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }
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
        public void LoadDirectory(ResourcePath path)
        {
            var sawmill = Logger.GetSawmill("eng");
            _hasEverBeenReloaded = true;
            var yamlStreams = _resources.ContentFindFiles(path).ToList().AsParallel()
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."))
                .Select(filePath =>
                {
                    try
                    {
                        using var reader = new StreamReader(_resources.ContentFileRead(filePath), EncodingHelpers.UTF8);
                        var yamlStream = new YamlStream();
                        yamlStream.Load(reader);

                        var result = ((YamlStream? yamlStream, ResourcePath?))(yamlStream, filePath);

                        LoadedData?.Invoke(yamlStream, filePath.ToString());

                        return result;
                    }
                    catch (YamlException e)
                    {
                        sawmill.Error("YamlException whilst loading prototypes from {0}: {1}", filePath, e.Message);
                        return (null, null);
                    }
                })
                .Where(p => p.yamlStream != null) // Filter out loading errors.
                .ToList();

            foreach (var (stream, filePath) in yamlStreams)
            {
                for (var i = 0; i < stream!.Documents.Count; i++)
                {
                    try
                    {
                        LoadFromDocument(stream.Documents[i]);
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("eng", $"Exception whilst loading prototypes from {filePath}#{i}:\n{e}");
                    }
                }
            }
        }

        public void LoadFromStream(TextReader stream)
        {
            _hasEverBeenReloaded = true;
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

            LoadedData?.Invoke(yaml, "anonymous prototypes YAML stream");
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
                RegisterType(type);
            }
        }

        private void LoadFromDocument(YamlDocument document)
        {
            var rootNode = (YamlSequenceNode)document.RootNode;
            foreach (YamlMappingNode node in rootNode.Cast<YamlMappingNode>())
            {
                var type = node.GetNode("type").AsString();
                if (!prototypeTypes.ContainsKey(type))
                {
                    if (IgnoredPrototypeTypes.Contains(type))
                    {
                        continue;
                    }
                    throw new PrototypeLoadException(string.Format("Unknown prototype type: '{0}'", type));
                }

                var prototypeType = prototypeTypes[type];
                var prototype = (IPrototype) _serv3Manager.ReadValue(prototypeType, node.ToDataNode());
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

        public bool TryIndex<T>(string id, [MaybeNullWhen(false)] out T prototype) where T : IIndexedPrototype
        {
            if (!indexedPrototypes.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }
            var returned = index.TryGetValue(id, out var uncast);
            prototype = (T) uncast!;
            return returned;
        }

        public void RegisterIgnore(string name)
        {
            IgnoredPrototypeTypes.Add(name);
        }

        /// <inheritdoc />
        public void RegisterType(Type type)
        {
            if(!(typeof(IPrototype).IsAssignableFrom(type)))
                throw new InvalidOperationException("Type must implement IPrototype.");

            var attribute = (PrototypeAttribute?)Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute));

            if (attribute == null)
            {
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    "No " + nameof(PrototypeAttribute) + " to give it a type string.");
            }

            if (prototypeTypes.ContainsKey(attribute.Type))
            {
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    $"Duplicate prototype type ID: {attribute.Type}. Current: {prototypeTypes[attribute.Type]}");
            }

            prototypeTypes[attribute.Type] = type;
            prototypes[type] = new List<IPrototype>();

            if (typeof(IIndexedPrototype).IsAssignableFrom(type))
            {
                indexedPrototypes[type] = new Dictionary<string, IIndexedPrototype>();
            }
        }

        public event Action<YamlStream, string>? LoadedData;

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
        public readonly string? Prototype;
        public UnknownPrototypeException(string prototype)
        {
            Prototype = prototype;
        }

        public UnknownPrototypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Prototype = (string?)info.GetValue("prototype", typeof(string));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("prototype", Prototype, typeof(string));
        }
    }
}
