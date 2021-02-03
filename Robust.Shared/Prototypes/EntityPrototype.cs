using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Prototype that represents game entities.
    /// </summary>
    [Prototype("entity")]
    public class EntityPrototype : IPrototype, IIndexedPrototype, ISyncingPrototype
    {
        /// <summary>
        /// The "in code name" of the object. Must be unique.
        /// </summary>
        [ViewVariables]
        public string ID { get; private set; } = default!;

        /// <summary>
        /// The "in game name" of the object. What is displayed to most players.
        /// </summary>
        [ViewVariables, CanBeNull]
        public string Name { get; private set; } = "";

        private bool _nameModified;
        private bool _descriptionModified;

        /// <summary>
        ///     Optional suffix to display in development menus like the entity spawn panel,
        ///     to provide additional info without ruining the Name property itself.
        /// </summary>
        [ViewVariables]
        public string? EditorSuffix { get; private set; }

        /// <summary>
        /// The description of the object that shows upon using examine
        /// </summary>
        [ViewVariables]
        public string Description { get; private set; } = "";

        /// <summary>
        ///     If true, this object should not show up in the entity spawn panel.
        /// </summary>
        [ViewVariables]
        public bool Abstract { get; private set; }

        /// <summary>
        /// The different mounting points on walls. (If any).
        /// </summary>
        [ViewVariables]
        public List<int>? MountingPoints { get; private set; }

        /// <summary>
        /// The Placement mode used for client-initiated placement. This is used for admin and editor placement. The serverside version controls what type the server assigns in normal gameplay.
        /// </summary>
        [ViewVariables]
        public string PlacementMode { get; protected set; } = "PlaceFree";

        /// <summary>
        /// The Range this entity can be placed from. This is only used serverside since the server handles normal gameplay. The client uses unlimited range since it handles things like admin spawning and editing.
        /// </summary>
        [ViewVariables]
        public int PlacementRange { get; protected set; } = DEFAULT_RANGE;

        private const int DEFAULT_RANGE = 200;

        /// <summary>
        /// Set to hold snapping categories that this object has applied to it such as pipe/wire/wallmount
        /// </summary>
        private readonly HashSet<string> _snapFlags = new();

        private bool _snapOverriden = false;

        /// <summary>
        /// Offset that is added to the position when placing. (if any). Client only.
        /// </summary>
        [ViewVariables]
        public Vector2i PlacementOffset { get; protected set; }

        private bool _placementOverriden = false;

        /// <summary>
        /// True if this entity will be saved by the map loader.
        /// </summary>
        [ViewVariables]
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
        private string? parentTemp;

        /// <summary>
        /// A dictionary mapping the component type list to the YAML mapping containing their settings.
        /// </summary>
        public Dictionary<string, DataClass> Components { get; } = new();

        /// <summary>
        /// The mapping node inside the <c>data</c> field of the prototype. Null if no data field exists.
        /// </summary>
        public YamlMappingNode? DataNode { get; set; }

        private readonly HashSet<Type> ReferenceTypes = new();

        string? CurrentDeserializingComponent;

        readonly Dictionary<string, Dictionary<(string, Type), object?>> FieldCache =
            new();

        readonly Dictionary<string, object?> DataCache = new();

        public EntityPrototype()
        {
            var compDataMgr = IoCManager.Resolve<IComponentDataManager>();
            // Everybody gets a transform component!
            Components.Add("Transform", compDataMgr.GetEmptyComponentData("Transform"));
            // And a metadata component too!
            Components.Add("MetaData", compDataMgr.GetEmptyComponentData("MetaData"));
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").AsString();

            if (mapping.TryGetNode("name", out var node))
            {
                _nameModified = true;
                Name = Loc.GetString(node.AsString());
            }

            if (mapping.TryGetNode("parent", out node))
            {
                parentTemp = node.AsString();
            }

            // DESCRIPTION
            if (mapping.TryGetNode("description", out node))
            {
                _descriptionModified = true;
                Description = Loc.GetString(node.AsString());
            }

            if (mapping.TryGetNode("suffix", out node))
            {
                EditorSuffix = Loc.GetString(node.AsString());
            }

            // COMPONENTS
            if (mapping.TryGetNode<YamlSequenceNode>("components", out var componentsequence))
            {
                var factory = IoCManager.Resolve<IComponentFactory>();
                foreach (var componentMapping in componentsequence.Cast<YamlMappingNode>())
                {
                    ReadComponent(componentMapping, factory);
                }

                // Assert that there are no conflicting component references.
                foreach (var componentName in Components.Keys)
                {
                    var registration = factory.GetRegistration(componentName);
                    foreach (var type in registration.References)
                    {
                        if (ReferenceTypes.Contains(type))
                        {
                            throw new InvalidOperationException(
                                $"Duplicate component reference in prototype: '{type}'");
                        }

                        ReferenceTypes.Add(type);
                    }
                }
            }

            // DATA FIELD
            if (mapping.TryGetNode<YamlMappingNode>("data", out var dataMapping))
            {
                DataNode = dataMapping;
            }

            // PLACEMENT
            // TODO: move to a component or something. Shouldn't be a root part of prototypes IMO.
            if (mapping.TryGetNode<YamlMappingNode>("placement", out var placementMapping))
            {
                ReadPlacementProperties(placementMapping);
            }

            // SAVING
            if (mapping.TryGetNode("save", out node))
            {
                MapSavable = node.AsBool();
            }

            if (mapping.TryGetNode("abstract", out node))
            {
                Abstract = node.AsBool();
            }
        }

        private void ReadPlacementProperties(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode("mode", out var node))
            {
                PlacementMode = node.AsString();
                _placementOverriden = true;
            }

            if (mapping.TryGetNode("offset", out node))
            {
                PlacementOffset = node.AsVector2i();
                _placementOverriden = true;
            }

            if (mapping.TryGetNode<YamlSequenceNode>("nodes", out var sequence))
            {
                MountingPoints = sequence.Select(p => p.AsInt()).ToList();
            }

            if (mapping.TryGetNode("range", out node))
            {
                PlacementRange = node.AsInt();
            }

            // Reads snapping flags that this object holds that describe its properties to such as wire/pipe/wallmount, used to prevent certain stacked placement
            if (mapping.TryGetNode<YamlSequenceNode>("snap", out var snapSequence))
            {
                var flagsList = snapSequence.Select(p => p.AsString());
                foreach (var flag in flagsList)
                {
                    _snapFlags.Add(flag);
                }

                _snapOverriden = true;
            }
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

        private static void PushInheritance(EntityPrototype source, EntityPrototype target)
        {
            var dataMgr = IoCManager.Resolve<IComponentDataManager>();
            // Copy component data over.
            foreach (var(type, component) in source.Components)
            {
                if (target.Components.TryGetValue(type, out var targetComponent))
                {
                    // Copy over values the target component does not have.
                    dataMgr.PushInheritance(type, component, targetComponent);
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

                    var data = dataMgr.GetEmptyComponentData(type);
                    dataMgr.PushInheritance(type, component, data);

                    target.Components[type] = data;
                }

                next: ;
            }

            // Copy all simple data over.
            if (!target._placementOverriden)
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
            }

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

        private static void EnsureCompExistsAndDeserialize(Entity entity, IComponentFactory factory, string compName, DataClass data)
        {
            var compType = factory.GetRegistration(compName).Type;

            if (!entity.TryGetComponent(compType, out var component))
            {
                var newComponent = (Component) factory.GetComponent(compName);
                newComponent.Owner = entity;
                entity.AddComponent(newComponent);
                component = newComponent;
            }

            IoCManager.Resolve<IComponentDataManager>().PopulateComponent(component, data);
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

            var data = IoCManager.Resolve<IComponentDataManager>().ParseComponentData(type, copy); //todo handle cached fields

            Components[type] = data;
        }

        public override string ToString()
        {
            return $"EntityPrototype({ID})";
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
