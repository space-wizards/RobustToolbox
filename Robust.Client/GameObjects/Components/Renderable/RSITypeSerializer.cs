using System;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.GameObjects.Components.Renderable
{
    public class RSITypeSerializer : YamlObjectSerializer.TypeSerializer
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public RSITypeSerializer()
        {
            IoCManager.InjectDependencies(this);
        }

        public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
        {
            var rsi = serializer.NodeToType<string>(node);

            if (!string.IsNullOrWhiteSpace(rsi))
            {
                var rsiPath = SharedSpriteComponent.TextureRoot / rsi;
                try
                {
                    return _resourceCache.GetResource<RSIResource>(rsiPath).RSI;
                }
                catch (Exception e)
                {
                    Logger.ErrorS(SpriteComponent.LogCategory, "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, e);
                }
            }

            return _resourceCache.GetFallback<RSIResource>();
        }

        public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
        {
            switch (obj)
            {
                case RSI rsi:
                    if (rsi.Path != null)
                    {
                        return new YamlMappingNode
                        {
                            {"", rsi.Path.Filename}
                        };
                    }
                    return new YamlMappingNode();
                default:
                    return new YamlMappingNode();
            }
        }
    }
}
