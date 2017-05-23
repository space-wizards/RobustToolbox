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
        private Dictionary<string, YamlMappingNode> components = new Dictionary<string, YamlMappingNode>();

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

        public Entity CreateEntity(EntityManager manager)
        {
            var entity = new Entity(manager);

            foreach (KeyValuePair<string, YamlMappingNode> componentData in components)
            {
                IComponent component = manager.ComponentFactory.GetComponent(componentData.Key);

            }

            return entity;
        }

        private void ReadComponent(YamlMappingNode mapping)
        {
            // TODO: nonexistant component types are not checked here.
            var type = ((YamlScalarNode)mapping[new YamlScalarNode("type")]).Value;

            components[type] = mapping;
            // TODO: figure out a better way to exclude the type node.
            mapping.Children.Remove(new YamlScalarNode("type"));
        }
    }
}
