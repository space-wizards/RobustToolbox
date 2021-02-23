using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Log;
using Robust.Shared.Network;
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
        void Initialize();

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
        /// Index for a <see cref="IPrototype"/> by ID.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the type of prototype is not registered.
        /// </exception>
        T Index<T>(string id) where T : class, IPrototype;

        /// <summary>
        /// Index for a <see cref="IPrototype"/> by ID.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the ID does not exist or the type of prototype is not registered.
        /// </exception>
        IPrototype Index(Type type, string id);
        bool HasIndex<T>(string id) where T : IPrototype;
        bool TryIndex<T>(string id, out T prototype) where T : IPrototype;

        /// <summary>
        /// Load prototypes from files in a directory, recursively.
        /// </summary>
        Task<List<IPrototype>> LoadDirectory(ResourcePath path);

        List<IPrototype> LoadFromStream(TextReader stream);

        List<IPrototype> LoadString(string str);

        /// <summary>
        /// Clear out all prototypes and reset to a blank slate.
        /// </summary>
        void Clear();

        /// <summary>
        ///     Performs a reload on all prototypes, updating the game state accordingly
        /// </summary>
        void ReloadPrototypes(ResourcePath file);

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
        [Dependency] public readonly IResourceManager Resources = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] public readonly ITaskManager TaskManager = default!;
        [Dependency] public readonly INetManager NetManager = default!;
        [Dependency] private readonly IServ3Manager _serv3Manager = default!;

        private readonly Dictionary<string, Type> prototypeTypes = new();

        private bool _initialized;
        private bool _hasEverBeenReloaded;
        private bool _hasEverResynced;

        #region IPrototypeManager members
        private readonly Dictionary<Type, Dictionary<string, IPrototype>> prototypes = new();

        private readonly HashSet<string> IgnoredPrototypeTypes = new();

        public virtual void Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException($"{nameof(PrototypeManager)} has already been initialized.");
            }

            _initialized = true;
        }

        public IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return prototypes[typeof(T)].Values.Select(p => (T) p);
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(Type type)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return prototypes[type].Values;
        }

        public T Index<T>(string id) where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }
            try
            {
                return (T)prototypes[typeof(T)][id];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownPrototypeException(id);
            }
        }

        public IPrototype Index(Type type, string id)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return prototypes[type][id];
        }

        public void Clear()
        {
            prototypeTypes.Clear();
            prototypes.Clear();
        }

        public virtual async void ReloadPrototypes(ResourcePath file)
        {
#if !FULL_RELEASE
            var changed = await LoadFile(file.ToRootedPath(), true);
            Resync();

            foreach (var prototype in changed)
            {
                if (prototype is not EntityPrototype entityPrototype)
                {
                    continue;
                }

                foreach (var entity in _entityManager.GetEntities(new PredicateEntityQuery(e => e.Prototype != null && e.Prototype.ID == entityPrototype.ID)))
                {
                    entityPrototype.UpdateEntity((Entity) entity);
                }
            }
#endif
        }

        public void Resync()
        {
            // TODO Make this smarter and only resync changed prototypes
            if (_hasEverResynced)
            {
                foreach (var prototypeList in prototypes.Values)
                {
                    foreach (var prototype in prototypeList.Values)
                    {
                        if (prototype is ISyncingPrototype syncing)
                        {
                            syncing.Reset();
                        }
                    }
                }
            }

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
                List<ISyncingPrototype> currentRun = prototypes[type].Values.Select(p => (ISyncingPrototype) p).ToList();

                var stage = 0;
                // Outer loop to iterate stages.
                while (currentRun.Count > 0)
                {
                    // Increase positions to iterate over list.
                    // If we need to stick, i gets reduced down below.
                    for (var i = 0; i < currentRun.Count; i++)
                    {
                        ISyncingPrototype prototype = currentRun[i];
                        var result = prototype.Sync(this, stage);
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

            _hasEverResynced = true;
        }

        /// <inheritdoc />
        public async Task<List<IPrototype>> LoadDirectory(ResourcePath path)
        {
            var changedPrototypes = new List<IPrototype>();

            _hasEverBeenReloaded = true;
            var streams = Resources.ContentFindFiles(path).ToList().AsParallel()
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."));

            foreach (var resourcePath in streams)
            {
                var filePrototypes = await LoadFile(resourcePath);
                changedPrototypes.AddRange(filePrototypes);
            }

            return changedPrototypes;
        }

        private Task<StreamReader?> ReadFile(ResourcePath file, bool @throw = true)
        {
            var retries = 0;

            // This might be shit-code, but its pjb-responded-idk-when-asked shit-code.
            while (true)
            {
                try
                {
                    var reader = new StreamReader(Resources.ContentFileRead(file), EncodingHelpers.UTF8);

                    return Task.FromResult<StreamReader?>(reader);
                }
                catch (IOException e)
                {
                    if (retries > 10)
                    {
                        if (@throw)
                        {
                            throw;
                        }

                        Logger.Error($"Error reloading prototypes in file {file}.", e);
                        return Task.FromResult<StreamReader?>(null);
                    }

                    retries++;
                    Thread.Sleep(10);
                }
            }
        }

        public async Task<List<IPrototype>> LoadFile(ResourcePath file, bool overwrite = false)
        {
            var changedPrototypes = new List<IPrototype>();

            try
            {
                using var reader = await ReadFile(file, !overwrite);

                if (reader == null)
                {
                    return changedPrototypes;
                }

                var yamlStream = new YamlStream();
                yamlStream.Load(reader);

                LoadedData?.Invoke(yamlStream, file.ToString());

                for (var i = 0; i < yamlStream.Documents.Count; i++)
                {
                    try
                    {
                        var documentPrototypes = LoadFromDocument(yamlStream.Documents[i], overwrite);
                        changedPrototypes.AddRange(documentPrototypes);
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("eng", $"Exception whilst loading prototypes from {file}#{i}:\n{e}");
                    }
                }
            }
            catch (YamlException e)
            {
                var sawmill = Logger.GetSawmill("eng");
                sawmill.Error("YamlException whilst loading prototypes from {0}: {1}", file, e.Message);
            }

            return changedPrototypes;
        }

        public List<IPrototype> LoadFromStream(TextReader stream)
        {
            var changedPrototypes = new List<IPrototype>();
            _hasEverBeenReloaded = true;
            var yaml = new YamlStream();
            yaml.Load(stream);

            for (var i = 0; i < yaml.Documents.Count; i++)
            {
                try
                {
                    var documentPrototypes = LoadFromDocument(yaml.Documents[i]);
                    changedPrototypes.AddRange(documentPrototypes);
                }
                catch (Exception e)
                {
                    throw new PrototypeLoadException($"Failed to load prototypes from document#{i}", e);
                }
            }

            LoadedData?.Invoke(yaml, "anonymous prototypes YAML stream");

            return changedPrototypes;
        }

        public List<IPrototype> LoadString(string str)
        {
            return LoadFromStream(new StringReader(str));
        }

        #endregion IPrototypeManager members

        public void PostInject()
        {
            ReflectionManager.OnAssemblyAdded += (_, _) => ReloadPrototypeTypes();
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

        private List<IPrototype> LoadFromDocument(YamlDocument document, bool overwrite = false)
        {
            var changedPrototypes = new List<IPrototype>();
            var rootNode = (YamlSequenceNode) document.RootNode;

            foreach (YamlMappingNode node in rootNode.Cast<YamlMappingNode>())
            {
                var type = node.GetNode("type").AsString();
                if (!prototypeTypes.ContainsKey(type))
                {
                    if (IgnoredPrototypeTypes.Contains(type))
                    {
                        continue;
                    }

                    throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
                }

                var prototypeType = prototypeTypes[type];
                var prototype = (IPrototype) _serv3Manager.ReadValue(prototypeType, node.ToDataNode());

                changedPrototypes.Add(prototype);

                var id = prototype.ID;

                if (!overwrite && prototypes[prototypeType].ContainsKey(id))
                {
                    throw new PrototypeLoadException($"Duplicate ID: '{id}'");
                }

                prototypes[prototypeType][id] = prototype;
            }

            return changedPrototypes;
        }

        public bool HasIndex<T>(string id) where T : IPrototype
        {
            if (!prototypes.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }
            return index.ContainsKey(id);
        }

        public bool TryIndex<T>(string id, [MaybeNullWhen(false)] out T prototype) where T : IPrototype
        {
            if (!prototypes.TryGetValue(typeof(T), out var index))
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

            if (typeof(IPrototype).IsAssignableFrom(type))
            {
                prototypes[type] = new Dictionary<string, IPrototype>();
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
