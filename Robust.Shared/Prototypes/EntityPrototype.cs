using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Prototypes
{

    /// <summary>
    /// Prototype that represents game entities.
    /// </summary>
    [Prototype(-1)]
    public sealed partial class EntityPrototype : IPrototype, IInheritingPrototype, ISerializationHooks
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

        [DataField("categories"), Access(typeof(PrototypeManager))]
        [NeverPushInheritance]
        internal HashSet<ProtoId<EntityCategoryPrototype>>? CategoriesInternal;

        /// <summary>
        /// What categories this prototype belongs to. This includes categories inherited from parents and categories
        /// that were automatically inferred from the prototype's components.
        /// </summary>
        [ViewVariables]
        public IReadOnlySet<EntityCategoryPrototype> Categories { get; internal set; } = new HashSet<EntityCategoryPrototype>();

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
        [Access(typeof(PrototypeManager))]
        public bool HideSpawnMenu { get; internal set; }

        [DataField("placement")]
        private EntityPlacementProperties PlacementProperties = new();

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

        [Obsolete("Pass in IComponentFactory")]
        public bool TryGetComponent<T>([NotNullWhen(true)] out T? component)
            where T : IComponent
        {
            var compName = IoCManager.Resolve<IComponentFactory>().GetComponentName(typeof(T));
            return TryGetComponent(compName, out component);
        }

        public bool TryGetComponent<T>([NotNullWhen(true)] out T? component, IComponentFactory factory) where T : IComponent, new()
        {
            var compName = factory.GetComponentName<T>();
            return TryGetComponent(compName, out component);
        }

        public bool TryGetComponent<T>(string name, [NotNullWhen(true)] out T? component) where T : IComponent
        {
            DebugTools.AssertEqual(IoCManager.Resolve<IComponentFactory>().GetComponentName(typeof(T)), name);

            if (!Components.TryGetValue(name, out var componentUnCast))
            {
                component = default;
                return false;
            }

            if (componentUnCast.Component is not T cast)
            {
                component = default;
                return false;
            }

            component = cast;
            return true;
        }

        internal static void LoadEntity(
            Entity<MetaDataComponent> ent,
            IComponentFactory factory,
            IEntityManager entityManager,
            ISerializationManager serManager,
            IEntityLoadContext? context) //yeah officer this method right here
        {
            var (entity, meta) = ent;
            var prototype = meta.EntityPrototype;
            var ctx = context as ISerializationContext;

            if (prototype != null)
            {
                foreach (var (name, entry) in prototype.Components)
                {
                    if (context != null && context.ShouldSkipComponent(name))
                        continue;

                    var fullData = context != null && context.TryGetComponent(name, out var data) ? data : entry.Component;
                    var compReg = factory.GetRegistration(name);
                    EnsureCompExistsAndDeserialize(entity, compReg, factory, entityManager, serManager, name, fullData, ctx);

                    if (!entry.Component.NetSyncEnabled && compReg.NetID is {} netId)
                        meta.NetComponents.Remove(netId);
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

                    if (!context.TryGetComponent(name, out var data))
                    {
                        throw new InvalidOperationException(
                            $"{nameof(IEntityLoadContext)} provided component name {name} but refused to provide data");
                    }

                    var compReg = factory.GetRegistration(name);
                    EnsureCompExistsAndDeserialize(entity, compReg, factory, entityManager, serManager, name, data, ctx);
                }
            }
        }

        public static void EnsureCompExistsAndDeserialize(EntityUid entity,
            ComponentRegistration compReg,
            IComponentFactory factory,
            IEntityManager entityManager,
            ISerializationManager serManager,
            string compName,
            IComponent data,
            ISerializationContext? context)
        {
            if (!entityManager.TryGetComponent(entity, compReg.Idx, out var component))
            {
                var newComponent = factory.GetComponent(compName);
                entityManager.AddComponent(entity, newComponent);
                component = newComponent;
            }

            if (context is not EntityDeserializer map)
            {
                serManager.CopyTo(data, ref component, context, notNullableOverride: true);
                return;
            }

            map.CurrentComponent = compName;
            serManager.CopyTo(data, ref component, context, notNullableOverride: true);
            map.CurrentComponent = null;
        }

        public override string ToString()
        {
            return $"EntityPrototype({ID})";
        }

        [DataRecord]
        public record ComponentRegistryEntry(IComponent Component, MappingDataNode Mapping);

        [DataDefinition]
        public sealed partial class EntityPlacementProperties
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

    public sealed class ComponentRegistry : Dictionary<string, EntityPrototype.ComponentRegistryEntry>, IEntityLoadContext
    {
        public ComponentRegistry()
        {
        }

        public ComponentRegistry(Dictionary<string, EntityPrototype.ComponentRegistryEntry> components) : base(components)
        {
        }

        public bool TryGetComponent(string componentName, [NotNullWhen(true)] out IComponent? component)
        {
            var success = TryGetValue(componentName, out var comp);
            component = comp?.Component;

            return success;
        }

        public IEnumerable<string> GetExtraComponentTypes()
        {
            return Keys;
        }

        public bool ShouldSkipComponent(string compName)
        {
            return false; //Registries cannot represent the "remove this component" state.
        }
    }
}
