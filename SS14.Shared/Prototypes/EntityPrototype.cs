using SFML.System;
using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.ContentLoader;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using YamlDotNet.RepresentationModel;
using SS14.Shared.Log;

namespace SS14.Shared.GameObjects
{
    [Prototype("entity")]
    [IoCTarget]
    public class EntityPrototype : IPrototype, IIndexedPrototype, ISyncingPrototype
    {
        /// <summary>
        /// The "in code name" of the object. Must be unique.
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// The "in game name" of the object. What is displayed to most players.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The type of entity instantiated when a new entity is created from this template.
        /// </summary>
        public Type ClassType { get; private set; }

        /// <summary>
        /// The different mounting points on walls. (If any).
        /// </summary>
        public List<int> MountingPoints { get; private set; }

        /// <summary>
        /// The Placement mode used for client-initiated placement. This is used for admin and editor placement. The serverside version controls what type the server assigns in normal gameplay.
        /// </summary>
        public string PlacementMode
        {
            get { return placementMode; }
            protected set { placementMode = value; }
        }
        private string placementMode = "AlignNone";

        /// <summary>
        /// The Range this entity can be placed from. This is only used serverside since the server handles normal gameplay. The client uses unlimited range since it handles things like admin spawning and editing.
        /// </summary>
        public int PlacementRange
        {
            get { return placementRange; }
            protected set { placementRange = value; }
        }
        private int placementRange = 200;

        /// <summary>
        /// Offset that is added to the position when placing. (if any). Client only.
        /// </summary>
        public Vector2i PlacementOffset { get; protected set; }

        /// <summary>
        /// The prototype we inherit from.
        /// </summary>
        public EntityPrototype Parent { get; private set; }

        /// <summary>
        /// A list of children inheriting from this prototype.
        /// </summary>
        public List<EntityPrototype> Children { get; private set; }
        public bool IsRoot => Parent == null;

        /// <summary>
        /// Used to store the parent id until we sync when all templates are done loading.
        /// </summary>
        private string parentTemp;

        /// <summary>
        /// A dictionary mapping the component type list to the YAML mapping containing their settings.
        /// </summary>
        private Dictionary<string, Dictionary<string, YamlNode>> components = new Dictionary<string, Dictionary<string, YamlNode>>();
        public Dictionary<string, Dictionary<string, YamlNode>> Components => components;

        public void LoadFrom(YamlMappingNode mapping)
        {
            YamlScalarNode idNode = (YamlScalarNode)mapping[new YamlScalarNode("id")];
            ID = idNode.Value;

            YamlNode node;

            if (mapping.Children.TryGetValue(new YamlScalarNode("name"), out node))
            {
                Name = node.AsString();
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("class"), out node))
            {
                var manager = IoCManager.Resolve<IContentLoader>();
                ClassType = manager.GetType(((YamlScalarNode)node).Value);
                // TODO: logging of when the ClassType doesn't exist: Safety for typos.
            }
            else
            {
                ClassType = typeof(Entity);
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("parent"), out node))
            {
                parentTemp = ((YamlScalarNode)node).Value;
            }

            // COMPONENTS
            if (mapping.Children.TryGetValue(new YamlScalarNode("components"), out node))
            {
                var sequence = (YamlSequenceNode)node;
                foreach (var componentMapping in sequence.Select((YamlNode n) => (YamlMappingNode)n))
                {
                    ReadComponent(componentMapping);
                }
            }

            // PLACEMENT
            // TODO: move to a component or something. Shouldn't be a root part of prototypes IMO.
            if (mapping.Children.TryGetValue(new YamlScalarNode("placement"), out node))
            {
                ReadPlacementProperties((YamlMappingNode)node);
            }
        }

        private void ReadPlacementProperties(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.Children.TryGetValue(new YamlScalarNode("mode"), out node))
            {
                PlacementMode = node.AsString();
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("offset"), out node))
            {
                PlacementOffset = node.AsVector2i();
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("nodes"), out node))
            {
                MountingPoints = new List<int>();
                foreach (YamlScalarNode point in ((YamlSequenceNode)node).Cast<YamlScalarNode>())
                {
                    MountingPoints.Add(point.AsInt());
                }
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("range"), out node))
            {
                PlacementRange = node.AsInt();
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
                    foreach (EntityPrototype child in Children)
                    {
                        PushInheritance(this, child);
                    }

                    break;
            }
            return false;
        }

        private static void PushInheritance(EntityPrototype source, EntityPrototype target)
        {
            foreach (KeyValuePair<string, Dictionary<string, YamlNode>> component in source.Components)
            {
                Dictionary<string, YamlNode> targetComponent;
                if (target.Components.TryGetValue(component.Key, out targetComponent))
                {
                    // Copy over values the target component does not have.
                    foreach (string key in component.Value.Keys)
                    {
                        if (!targetComponent.ContainsKey(key))
                        {
                            targetComponent[key] = component.Value[key];
                        }
                    }
                }
                else
                {
                    // Copy component into the target, since it doesn't have it yet.
                    target.Components[component.Key] = new Dictionary<string, YamlNode>(component.Value);
                }
            }

            if (target.Name == null)
            {
                target.Name = source.Name;
            }

            if (target.Children == null)
            {
                return;
            }

            // TODO: remove recursion somehow.
            foreach (EntityPrototype child in target.Children)
            {
                PushInheritance(target, child);
            }
        }

        /// <summary>
        /// Creates an entity from this template
        /// </summary>
        /// <returns></returns>
        public IEntity CreateEntity(IEntityManager manager, IEntityNetworkManager networkManager, IComponentFactory componentFactory)
        {
            var entity = (IEntity)Activator.CreateInstance(ClassType, manager, networkManager);

            foreach (KeyValuePair<string, Dictionary<string, YamlNode>> componentData in components)
            {
                IComponent component;
                try
                {
                    component = componentFactory.GetComponent(componentData.Key);
                }
                catch (UnknowComponentException)
                {
                    // Ignore nonexistant ones.
                    // This is kind of inefficient but we'll do the sanity on prototype creation
                    // Once the dependency injection stack is fixed.
                    LogManager.Log(string.Format("Unable to load prototype component. UnknowComponentException occured for componentKey `{0}`", componentData.Key));
                    continue;
                }
                component.LoadParameters(componentData.Value);

                entity.AddComponent(component.Family, component);
            }

            entity.Name = Name;
            entity.Prototype = this;
            return entity;
        }

        // 100% completely refined & distilled cancer.
        public IEnumerable<ComponentParameter> GetBaseSpriteParamaters()
        {
            // Emotional programming.
            Dictionary<string, YamlNode> ಠ_ಠ;
            if (components.TryGetValue("Icon", out ಠ_ಠ))
            {
                YamlNode ಥ_ಥ;
                if (ಠ_ಠ.TryGetValue("icon", out ಥ_ಥ) && ಥ_ಥ is YamlScalarNode)
                {
                    return new ComponentParameter[] { new ComponentParameter("icon", ಥ_ಥ.AsString()) };
                }
            }
            return new ComponentParameter[0];
        }

        private void ReadComponent(YamlMappingNode mapping)
        {
            // TODO: nonexistant component types are not checked here.
            var type = ((YamlScalarNode)mapping[new YamlScalarNode("type")]).Value;
            Dictionary<string, YamlNode> dict = YamlHelpers.YamlMappingToDict(mapping);
            dict.Remove("type");

            components[type] = dict;
            // TODO: figure out a better way to exclude the type node.
        }

        public override string ToString()
        {
            return string.Format("EntityPrototype({0})", ID);
        }
    }
}
