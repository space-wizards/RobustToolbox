using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes
{
    /// <summary>
    /// Prototype that represents game entities.
    /// </summary>
    [Prototype("entity")]
    public class EntityPrototype : IPrototype, ISyncingPrototype
    {
        /// <summary>
        /// The "in code name" of the object. Must be unique.
        /// </summary>
        [ViewVariables]
        [DataField("id")]
        public string ID { get; private set; } = default!;

        /// <summary>
        /// The "in game name" of the object. What is displayed to most players.
        /// </summary>
        [ViewVariables, CanBeNull]
        [DataField("name")]
        public string Name {
            get => _name;
            private set
            {
                _nameModified = true;
                _name = Loc.GetString(value);
            }
        }

        private string _name = "";

        private bool _nameModified;
        private bool _descriptionModified;

        /// <summary>
        ///     Optional suffix to display in development menus like the entity spawn panel,
        ///     to provide additional info without ruining the Name property itself.
        /// </summary>
        [ViewVariables]
        [DataField("suffix")]
        public string? EditorSuffix
        {
            get => _editorSuffix;
            private set { _editorSuffix = value != null ? Loc.GetString(value) : null; }
        }
        private string? _editorSuffix;

        /// <summary>
        /// The description of the object that shows upon using examine
        /// </summary>
        [ViewVariables]
        [DataField("description")]
        public string Description
        {
            get => _description;
            private set
            {
                _descriptionModified = true;
                _description = Loc.GetString(value);
            }

        }
        private string _description = "";

        /// <summary>
        ///     If true, this object should not show up in the entity spawn panel.
        /// </summary>
        [ViewVariables]
        [DataField("abstract")]
        public bool Abstract { get; private set; }

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

        private const int DEFAULT_RANGE = 200;

        /// <summary>
        /// Set to hold snapping categories that this object has applied to it such as pipe/wire/wallmount
        /// </summary>
        private HashSet<string> _snapFlags => PlacementProperties.SnapFlags;

        private bool _snapOverriden => PlacementProperties.SnapOverriden;

        /// <summary>
        /// Offset that is added to the position when placing. (if any). Client only.
        /// </summary>
        [ViewVariables]
        public Vector2i PlacementOffset => PlacementProperties.PlacementOffset;

        private bool _placementOverriden => PlacementProperties.PlacementOverriden;

        /// <summary>
        /// True if this entity will be saved by the map loader.
        /// </summary>
        [ViewVariables]
        [DataField("save")]
        public bool MapSavable { get; protected set; } = true;

        /// <summary>
        /// The prototype we inherit from.
        /// </summary>
        [ViewVariables]
        public EntityPrototype? Parent { get; private set; }

        /// <summary>
        /// A list of children inheriting from this prototype.
        /// </summary>
        [ViewVariables]
        public List<EntityPrototype> Children { get; private set; } = new();

        public bool IsRoot => Parent == null;

        /// <summary>
        /// Used to store the parent id until we sync when all templates are done loading.
        /// </summary>
        [DataField("parent")]
        private string? parentTemp;

        /// <summary>
        /// A dictionary mapping the component type list to the YAML mapping containing their settings.
        /// </summary>
        [field: DataField("components")]
        public ComponentRegistry Components { get; } = new();

        private readonly HashSet<Type> ReferenceTypes = new();

        string? CurrentDeserializingComponent;

        readonly Dictionary<string, Dictionary<(string, Type), object?>> FieldCache =
            new();

        readonly Dictionary<string, object?> DataCache = new();

        public EntityPrototype()
        {
            // Everybody gets a transform component!
            Components.Add("Transform", new TransformComponent());
            // And a metadata component too!
            Components.Add("MetaData", new MetaDataComponent());
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
            component = (T) componentUnCast;
            return true;
        }

        public void Reset()
        {
            Children.Clear();
        }

        // Resolve inheritance.
        public bool Sync(IPrototypeManager manager, int stage)
        {
            switch (stage)
            {
                case 0:
                    if (parentTemp == null)
                    {
                        return true;
                    }

                    Parent = manager.Index<EntityPrototype>(parentTemp);
                    if (Parent.Children == null)
                    {
                        Parent.Children = new List<EntityPrototype>();
                    }

                    Parent.Children.Add(this);
                    return false;

                case 1:
                    // We are a root-level prototype.
                    // As such we're getting the duty of pushing inheritance into everybody's face.
                    // Can't do a "puller" system where each queries the parent because it requires n stages
                    //  (n being the depth of each inheritance tree)

                    if (Children == null)
                    {
                        break;
                    }

                    PushInheritanceAll();
                    break;
            }

            return false;
        }

        /// <summary>
        /// Iteratively pushes inheritance down to all children, children's children, etc. breadth-first.
        /// </summary>
        private void PushInheritanceAll()
        {
            if (Children == null)
            {
                return;
            }

            var sourceTargets = new List<(EntityPrototype, List<EntityPrototype>)> {(this, Children)};
            var newSources = new List<EntityPrototype>();
            while (true)
            {
                foreach (var (source, targetList) in sourceTargets)
                {
                    if (targetList == null)
                    {
                        continue;
                    }

                    foreach (var target in targetList)
                    {
                        PushInheritance(source, target);
                    }

                    newSources.AddRange(targetList);
                }

                if (newSources.Count == 0)
                {
                    break;
                }

                sourceTargets.Clear();
                foreach (var newSource in newSources)
                {
                    sourceTargets.Add((newSource, newSource.Children));
                }

                newSources.Clear();
            }
        }

        //todo paul remove
        private static void PushInheritance(EntityPrototype source, EntityPrototype target)
        {
            var serializationManager = IoCManager.Resolve<ISerializationManager>();
            // Copy component data over.
            foreach (var(type, component) in source.Components)
            {
                if (target.Components.TryGetValue(type, out var targetComponent))
                {
                    // Copy over values the target component does not have.
                    //todo paul
                    targetComponent = serializationManager.Copy(component, targetComponent)!;
                }
                else
                {
                    // Copy component into the target, since it doesn't have it yet.
                    // Unless it'd cause a conflict.
                    var factory = IoCManager.Resolve<IComponentFactory>();
                    foreach (var refType in factory.GetRegistration(type).References)
                    {
                        if (target.ReferenceTypes.Contains(refType))
                        {
                            // yeah nope the child's got it!! NEXT!!
                            goto next;
                        }
                    }


                    var data = serializationManager.CreateCopy(component)!;

                    target.Components[type] = (IComponent)data;
                }

                next: ;
            }

            target.PlacementProperties = serializationManager.Copy(source.PlacementProperties, target.PlacementProperties)!;
            //target.PlacementProperties = (EntityPlacementProperties)serv3Mgr.CreateCopy(source.PlacementProperties);
            // Copy all simple data over.
            /*if (!target._placementOverriden)
            {
                target.PlacementMode = source.PlacementMode;
            }

            if (!target._placementOverriden)
            {
                target.PlacementOffset = source.PlacementOffset;
            }

            if (target.MountingPoints == null && source.MountingPoints != null)
            {
                target.MountingPoints = new List<int>(source.MountingPoints);
            }

            if (target.PlacementRange == DEFAULT_RANGE)
            {
                target.PlacementRange = source.PlacementRange;
            }*/

            if (!target._descriptionModified)
            {
                target.Description = source.Description;
            }

            if (target.EditorSuffix == null)
            {
                target.EditorSuffix = source.EditorSuffix;
            }

            if (!target._snapOverriden)
            {
                foreach (var flag in source._snapFlags)
                {
                    target._snapFlags.Add(flag);
                }
            }

            if (!target._nameModified)
            {
                target.Name = source.Name;
            }

            if (target.Children == null)
            {
                return;
            }
        }

        public void UpdateEntity(Entity entity)
        {
            if (ID != entity.Prototype?.ID)
            {
                Logger.Error($"Reloaded prototype used to update entity did not match entity's existing prototype: Expected '{ID}', got '{entity.Prototype?.ID}'");
                return;
            }

            var factory = IoCManager.Resolve<IComponentFactory>();
            var componentManager = IoCManager.Resolve<IComponentManager>();
            var oldPrototype = entity.Prototype;

            var oldPrototypeComponents = oldPrototype.Components.Keys
                .Where(n => n != "Transform" && n != "MetaData")
                .Select(name => (name, factory.GetRegistration(name).Type))
                .ToList();
            var newPrototypeComponents = Components.Keys
                .Where(n => n != "Transform" && n != "MetaData")
                .Select(name => (name, factory.GetRegistration(name).Type))
                .ToList();

            var ignoredComponents = new List<string>();

            // Find components to be removed, and remove them
            foreach (var (name, type) in oldPrototypeComponents.Except(newPrototypeComponents))
            {
                if (Components.Keys.Contains(name))
                {
                    ignoredComponents.Add(name);
                    continue;
                }

                componentManager.RemoveComponent(entity.Uid, type);
            }

            componentManager.CullRemovedComponents();

            var componentDependencyManager = IoCManager.Resolve<IComponentDependencyManager>();

            // Add new components
            foreach (var (name, type) in newPrototypeComponents.Where(t => !ignoredComponents.Contains(t.name)).Except(oldPrototypeComponents))
            {
                var data = Components[name];
                var component = (Component) factory.GetComponent(name);
                CurrentDeserializingComponent = name;
                component.Owner = entity;
                componentDependencyManager.OnComponentAdd(entity, component);
                entity.AddComponent(component);
            }

            // Update entity metadata
            entity.MetaData.EntityPrototype = this;
        }

        internal static void LoadEntity(EntityPrototype? prototype, Entity entity, IComponentFactory factory, IEntityLoadContext? context) //yeah officer this method right here
        {
            /*YamlObjectSerializer.Context? defaultContext = null;
            if (context == null)
            {
                defaultContext = new PrototypeSerializationContext(prototype);
            }*/

            if (prototype != null)
            {
                foreach (var (name, data) in prototype.Components)
                {
                    var fullData = data;
                    if (context != null)
                    {
                        fullData = context.GetComponentData(name, data);
                    }
                    /*else
                    {
                        prototype.CurrentDeserializingComponent = name;
                        ser = YamlObjectSerializer.NewReader(data, defaultContext);
                    }*/
                    //ser.CurrentType = factory.GetRegistration(name).Type;

                    //var contextData = IoCManager.Resolve<IComponentDataManager>().ParseComponentData(name, )

                    EnsureCompExistsAndDeserialize(entity, factory, name, fullData);
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

                    EnsureCompExistsAndDeserialize(entity, factory, name, ser);
                }
            }
        }

        private static void EnsureCompExistsAndDeserialize(Entity entity, IComponentFactory factory, string compName, IComponent data)
        {
            var compType = factory.GetRegistration(compName).Type;

            if (!entity.TryGetComponent(compType, out var component))
            {
                var newComponent = (Component) factory.GetComponent(compName);
                newComponent.Owner = entity;
                entity.AddComponent(newComponent);
                component = newComponent;
            }

            IoCManager.Resolve<ISerializationManager>().Copy(data, component);
        }

        private void ReadComponent(YamlMappingNode mapping, IComponentFactory factory)
        {
            string type = mapping.GetNode("type").AsString();
            // See if type exists to detect errors.
            switch (factory.GetComponentAvailability(type))
            {
                case ComponentAvailability.Available:
                    break;

                case ComponentAvailability.Ignore:
                    return;

                case ComponentAvailability.Unknown:
                    Log.Logger.Error($"Unknown component '{type}' in prototype {ID}!");
                    return;
            }

            // Has this type already been added?
            if (Components.Keys.Contains(type))
            {
                Log.Logger.Error($"Component of type '{type}' defined twice in prototype {ID}!");
                return;
            }

            var copy = new YamlMappingNode(mapping.AsEnumerable());
            // TODO: figure out a better way to exclude the type node.
            // Also maybe deep copy this? Right now it's pretty error prone.
            copy.Children.Remove(new YamlScalarNode("type"));
            var compType = factory.GetRegistration(type).Type;

            Components[type] = IoCManager.Resolve<ISerializationManager>().ReadValueOrThrow<IComponent>(compType, copy.ToDataNode());
        }

        public override string ToString()
        {
            return $"EntityPrototype({ID})";
        }

        public class ComponentRegistry : Dictionary<string, IComponent>
        {
            public ComponentRegistry()
            {
            }

            public ComponentRegistry(Dictionary<string, IComponent> components) : base(components)
            {
            }
        }

        [DataDefinition]
        public class EntityPlacementProperties
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
            private HashSet<string> _snapFlags = new ();

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
