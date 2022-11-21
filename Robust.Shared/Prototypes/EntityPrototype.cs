using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Prototypes
{
    /// <summary>
    /// Prototype that represents game entities.
    /// </summary>
    [Prototype("entity", -1)]
    public sealed class EntityPrototype : IPrototype, IInheritingPrototype, ISerializationHooks
    {
        private ILocalizationManager _loc = default!;

        private static readonly Dictionary<string, string> LocPropertiesDefault = new();

        // LOCALIZATION NOTE:
        // Localization-related properties in here are manually localized in LocalizationManager.
        // As such, they should NOT be inherited to avoid confusing the system.

        private const int DEFAULT_RANGE = 200;

        [DataField("loc")]
        private Dictionary<string, string>? _locPropertiesSet;

        /// <summary>
        /// The "in code name" of the object. Must be unique.
        /// </summary>
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     The name set on this level of the prototype. This does NOT handle localization or inheritance.
        ///     You probably want <see cref="Name"/> instead.
        /// </summary>
        /// <seealso cref="Name"/>
        [DataField("name")]
        public string? SetName { get; private set; }

        [DataField("description")]
        public string? SetDesc { get; private set; }

        [DataField("suffix")]
        public string? SetSuffix { get; private set; }

        [ViewVariables]
        public IReadOnlyDictionary<string, string> LocProperties => _locPropertiesSet ?? LocPropertiesDefault;

        /// <summary>
        /// The "in game name" of the object. What is displayed to most players.
        /// </summary>
        [ViewVariables]
        public string Name => _loc.GetEntityData(ID).Name;

        /// <summary>
        /// The description of the object that shows upon using examine
        /// </summary>
        [ViewVariables]
        public string Description => _loc.GetEntityData(ID).Desc;

        /// <summary>
        ///     Optional suffix to display in development menus like the entity spawn panel,
        ///     to provide additional info without ruining the Name property itself.
        /// </summary>
        [ViewVariables]
        public string? EditorSuffix => _loc.GetEntityData(ID).Suffix;

        /// <summary>
        /// Fluent messageId used to lookup the entity's name and localization attributes.
        /// </summary>
        [DataField("localizationId")]
        public string? CustomLocalizationID { get; private set; }


        /// <summary>
        ///     If true, this object should not show up in the entity spawn panel.
        /// </summary>
        [ViewVariables]
        [NeverPushInheritance]
        [DataField("noSpawn")]
        public bool NoSpawn { get; private set; }

        [DataField("placement")] private EntityPlacementProperties PlacementProperties = new();

        /// <summary>
        /// The different mounting points on walls. (If any).
        /// </summary>
        [ViewVariables]
        public List<int>? MountingPoints => PlacementProperties.MountingPoints;

        /// <summary>
        /// The Placement mode used for client-initiated placement. This is used for admin and editor placement. The serverside version controls what type the server assigns in normal gameplay.
        /// </summary>
        [ViewVariables]
        public string PlacementMode => PlacementProperties.PlacementMode;

        /// <summary>
        /// The Range this entity can be placed from. This is only used serverside since the server handles normal gameplay. The client uses unlimited range since it handles things like admin spawning and editing.
        /// </summary>
        [ViewVariables]
        public int PlacementRange => PlacementProperties.PlacementRange;

        /// <summary>
        /// Offset that is added to the position when placing. (if any). Client only.
        /// </summary>
        [ViewVariables]
        public Vector2i PlacementOffset => PlacementProperties.PlacementOffset;

        /// <summary>
        /// True if this entity will be saved by the map loader.
        /// </summary>
        [DataField("save")]
        public bool MapSavable { get; set; } = true;

        /// <summary>
        /// The prototype we inherit from.
        /// </summary>
        [ViewVariables]
        [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<EntityPrototype>))]
        public string[]? Parents { get; }

        [ViewVariables]
        [NeverPushInheritance]
        [AbstractDataField]
        public bool Abstract { get; }

        /// <summary>
        /// A dictionary mapping the component type list to the YAML mapping containing their settings.
        /// </summary>
        [DataField("components")]
        [AlwaysPushInheritance]
        public ComponentRegistry Components { get; } = new();

        public EntityPrototype()
        {
            // Everybody gets a transform component!
            Components.Add("Transform", new ComponentRegistryEntry(new TransformComponent(), new MappingDataNode()));
            // And a metadata component too!
            Components.Add("MetaData", new ComponentRegistryEntry(new MetaDataComponent(), new MappingDataNode()));
        }

        void ISerializationHooks.AfterDeserialization()
        {
            _loc = IoCManager.Resolve<ILocalizationManager>();
        }

        public bool TryGetComponent<T>([NotNullWhen(true)] out T? component, IComponentFactory? factory = null) where T : IComponent
        {
            if (factory == null)
            {
                factory = IoCManager.Resolve<IComponentFactory>();
            }

            var compName = factory.GetComponentName(typeof(T));
            return TryGetComponent(compName, out component);
        }

        public bool TryGetComponent<T>(string name, [NotNullWhen(true)] out T? component) where T : IComponent
        {
            if (!Components.TryGetValue(name, out var componentUnCast))
            {
                component = default;
                return false;
            }

            // There are no duplicate component names
            // TODO Sanity check with names being in an attribute of the type instead
            component = (T) componentUnCast.Component;
            return true;
        }

        internal static void LoadEntity(
            EntityPrototype? prototype,
            EntityUid entity,
            IComponentFactory factory,
            IEntityManager entityManager,
            ISerializationManager serManager,
            IEntityLoadContext? context) //yeah officer this method right here
        {
            /*YamlObjectSerializer.Context? defaultContext = null;
            if (context == null)
            {
                defaultContext = new PrototypeSerializationContext(prototype);
            }*/

            if (prototype != null)
            {
                foreach (var (name, entry) in prototype.Components)
                {
                    if (context != null && context.ShouldSkipComponent(name))
                        continue;

                    var fullData = entry.Mapping;

                    if (context != null)
                        fullData = context.GetComponentData(name, fullData);

                    EnsureCompExistsAndDeserialize(entity, factory, entityManager, serManager, name, fullData, context as ISerializationContext);
                }
            }

            if (context != null)
            {
                foreach (var name in context.GetExtraComponentTypes())
                {
                    if (prototype != null && prototype.Components.ContainsKey(name))
                    {
                        // This component also exists in the prototype.
                        // This means that the previous step already caught both the prototype data AND map data.
                        // Meaning that re-running EnsureCompExistsAndDeserialize would wipe prototype data.
                        continue;
                    }

                    var ser = context.GetComponentData(name, null);

                    EnsureCompExistsAndDeserialize(entity, factory, entityManager, serManager, name, ser, context as ISerializationContext);
                }
            }
        }

        private static void EnsureCompExistsAndDeserialize(EntityUid entity,
            IComponentFactory factory,
            IEntityManager entityManager,
            ISerializationManager serManager,
            string compName,
            MappingDataNode data,
            ISerializationContext? context)
        {
            var compReg = factory.GetRegistration(compName);

            if (!entityManager.TryGetComponent(entity, compReg.Idx, out var component))
            {
                var newComponent = (Component) factory.GetComponent(compName);
                newComponent.Owner = entity;
                entityManager.AddComponent(entity, newComponent);
                component = newComponent;
            }

            // TODO use this value to support struct components
            serManager.Read(compReg.Type, data, context, value: component);
        }

        public override string ToString()
        {
            return $"EntityPrototype({ID})";
        }

        public sealed class ComponentRegistry : Dictionary<string, ComponentRegistryEntry>
        {
            public ComponentRegistry()
            {
            }

            public ComponentRegistry(Dictionary<string, ComponentRegistryEntry> components) : base(components)
            {
            }
        }

        public sealed class ComponentRegistryEntry
        {
            public readonly IComponent Component;
            // Mapping is just a quick reference to speed up entity creation.
            public readonly MappingDataNode Mapping;

            public ComponentRegistryEntry(IComponent component, MappingDataNode mapping)
            {
                Component = component;
                Mapping = mapping;
            }
        }

        [DataDefinition]
        public sealed class EntityPlacementProperties
        {
            public bool PlacementOverriden { get; private set; }
            public bool SnapOverriden { get; private set; }
            private string _placementMode = "PlaceFree";
            private Vector2i _placementOffset;

            [DataField("mode")]
            public string PlacementMode
            {
                get => _placementMode;
                set
                {
                    PlacementOverriden = true;
                    _placementMode = value;
                }
            }

            [DataField("offset")]
            public Vector2i PlacementOffset
            {
                get => _placementOffset;
                set
                {
                    PlacementOverriden = true;
                    _placementOffset = value;
                }
            }

            [DataField("nodes")] public List<int>? MountingPoints;

            [DataField("range")] public int PlacementRange = DEFAULT_RANGE;
            private HashSet<string> _snapFlags = new();

            [DataField("snap")]
            public HashSet<string> SnapFlags
            {
                get => _snapFlags;
                set
                {
                    SnapOverriden = true;
                    _snapFlags = value;
                }
            }
        }
        /*private class PrototypeSerializationContext : YamlObjectSerializer.Context
        {
            readonly EntityPrototype? prototype;

            public PrototypeSerializationContext(EntityPrototype? owner)
            {
                prototype = owner;
            }

            public override void SetCachedField<T>(string field, T value)
            {
                if (StackDepth != 0 || prototype?.CurrentDeserializingComponent == null)
                {
                    base.SetCachedField<T>(field, value);
                    return;
                }

                if (!prototype.FieldCache.TryGetValue(prototype.CurrentDeserializingComponent, out var fieldList))
                {
                    fieldList = new Dictionary<(string, Type), object?>();
                    prototype.FieldCache[prototype.CurrentDeserializingComponent] = fieldList;
                }

                fieldList[(field, typeof(T))] = value;
            }

            public override bool TryGetCachedField<T>(string field, [MaybeNullWhen(false)] out T value)
            {
                if (StackDepth != 0 || prototype?.CurrentDeserializingComponent == null)
                {
                    return base.TryGetCachedField<T>(field, out value);
                }

                if (prototype.FieldCache.TryGetValue(prototype.CurrentDeserializingComponent, out var dict))
                {
                    if (dict.TryGetValue((field, typeof(T)), out var theValue))
                    {
                        value = (T) theValue!;
                        return true;
                    }
                }

                value = default!;
                return false;
            }

            public override void SetDataCache(string field, object value)
            {
                if (StackDepth != 0 || prototype == null)
                {
                    base.SetDataCache(field, value);
                    return;
                }

                prototype.DataCache[field] = value;
            }

            public override bool TryGetDataCache(string field, out object? value)
            {
                if (StackDepth != 0 || prototype == null)
                {
                    return base.TryGetDataCache(field, out value);
                }

                return prototype.DataCache.TryGetValue(field, out value);
            }
        }*/
    }
}
