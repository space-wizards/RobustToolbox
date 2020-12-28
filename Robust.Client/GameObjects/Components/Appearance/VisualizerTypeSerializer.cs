using System;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.GameObjects
{
    public class VisualizerTypeSerializer : YamlObjectSerializer.TypeSerializer
    {
        private IReflectionManager? _reflectionManager;

        public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
        {
            _reflectionManager ??= IoCManager.Resolve<IReflectionManager>();
            var mapping = (YamlMappingNode) node;
            var nodeType = mapping.GetNode("type");
            switch (nodeType.AsString())
            {
                case AppearanceComponent.SpriteLayerToggle.NAME:
                    var keyString = mapping.GetNode("key").AsString();
                    object key;
                    if (_reflectionManager.TryParseEnumReference(keyString, out var @enum))
                    {
                        key = @enum;
                    }
                    else
                    {
                        key = keyString;
                    }

                    var layer = mapping.GetNode("layer").AsInt();
                    return new AppearanceComponent.SpriteLayerToggle(key, layer);

                default:
                    var visType = _reflectionManager.LooseGetType(nodeType.AsString());
                    if (!typeof(AppearanceVisualizer).IsAssignableFrom(visType))
                    {
                        throw new InvalidOperationException();
                    }

                    var vis = (AppearanceVisualizer) Activator.CreateInstance(visType)!;
                    vis.LoadData(mapping);
                    return vis;
            }
        }

        public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
        {
            _reflectionManager ??= IoCManager.Resolve<IReflectionManager>();
            switch (obj)
            {
                case AppearanceComponent.SpriteLayerToggle spriteLayerToggle:
                    YamlScalarNode key;
                    if (spriteLayerToggle.Key is Enum)
                    {
                        var name = spriteLayerToggle.Key.GetType().FullName;
                        key = new YamlScalarNode($"{name}.{spriteLayerToggle.Key}");
                    }
                    else
                    {
                        key = new YamlScalarNode(spriteLayerToggle.Key.ToString());
                    }

                    return new YamlMappingNode
                    {
                        {new YamlScalarNode("type"), new YamlScalarNode(AppearanceComponent.SpriteLayerToggle.NAME)},
                        {new YamlScalarNode("key"), key},
                        {new YamlScalarNode("layer"), new YamlScalarNode(spriteLayerToggle.SpriteLayer.ToString())},
                    };
                default:
                    // TODO: A proper way to do serialization here.
                    // I can't use the ExposeData system here since that's specific to entity serializers.
                    return new YamlMappingNode();
            }
        }
    }
}
