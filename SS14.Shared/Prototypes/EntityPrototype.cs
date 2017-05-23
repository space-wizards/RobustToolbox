using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;
using SS14.Shared.ContentLoader;
using SS14.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.GameObjects
{
    [Prototype("entity")]
    [IoCTarget]
    public class EntityPrototype : IPrototype, IIndexedPrototype, ISyncingPrototype
    {
        public string ID { get; private set; }
        public string Name { get; private set; }
        public Type ClassType { get; private set; }
        public EntityPrototype Parent { get; private set; }
        // Used to store the parent id until we sync when all templates are done loading.
        private string parentTemp;
        public IDictionary<Type, YamlMappingNode> Components => components;
        private Dictionary<Type, YamlMappingNode> components = new Dictionary<Type, YamlMappingNode>();

        private static Dictionary<string, Type> componentTypes;

        public EntityPrototype()
        {
            if (componentTypes == null)
            {
                ReloadComponents();
            }
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            YamlScalarNode idNode = (YamlScalarNode)mapping[new YamlScalarNode("id")];
            ID = idNode.Value;

            YamlScalarNode nameNode = (YamlScalarNode)mapping[new YamlScalarNode("name")];
            Name = nameNode.Value;

            YamlNode node;
            if (mapping.Children.TryGetValue(new YamlScalarNode("class"), out node))
            {
                var manager = IoCManager.Resolve<IContentLoader>();
                ClassType = manager.GetType(((YamlScalarNode)node).Value);
                // TODO: logging of when the ClassType doesn't exist: Safety for typos.
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("parent"), out node))
            {
                parentTemp = ((YamlScalarNode)node).Value;
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("components"), out node))
            {
                var sequence = (YamlSequenceNode)node;
                foreach (var componentMapping in sequence.Select((YamlNode n) => (YamlMappingNode)n))
                {
                    ReadComponent(componentMapping);
                }
            }
        }

        // Resolve parents.
        public void Sync(IPrototypeManager manager)
        {
            Parent = manager.Index<EntityPrototype>(parentTemp);
        }

        private void ReadComponent(YamlMappingNode mapping)
        {
            var type = ((YamlScalarNode)mapping[new YamlScalarNode("type")]).Value;
            var componentType = componentTypes[type];

            components[componentType] = mapping;
            // TODO: figure out a better way to exclude the type node.
            mapping.Children.Remove(new YamlScalarNode("type"));
        }

        private static void ReloadComponents()
        {
            if (componentTypes == null)
            {
                 componentTypes = new Dictionary<string, Type>();
            }
            foreach (var type in IoCManager.ResolveEnumerable<IComponent>())
            {
                var attribute = (ComponentAttribute)Attribute.GetCustomAttribute(type, typeof(ComponentAttribute));
                if (attribute == null)
                {
                    throw new InvalidImplementationException(type, typeof(ComponentAttribute), "No " + nameof(ComponentAttribute));
                }

                if (componentTypes.ContainsKey(attribute.ID))
                {
                    throw new Exception("Duplicate ID for component: " + attribute.ID);
                }

                componentTypes[attribute.ID] = type;
            }
        }
    }
}
