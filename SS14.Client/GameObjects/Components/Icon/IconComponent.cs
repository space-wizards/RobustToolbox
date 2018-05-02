using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    public class IconComponent : Component
    {
        public override string Name => "Icon";
        public IDirectionalTextureProvider Icon { get; private set; }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);
            Icon = TextureForConfig(mapping, Owner.Prototype);
        }

        private static IDirectionalTextureProvider TextureForConfig(YamlMappingNode mapping, EntityPrototype prototype)
        {
            var resc = IoCManager.Resolve<IResourceCache>();

            if (mapping.TryGetNode("texture", out YamlNode node))
            {
                return resc.GetResource<TextureResource>(SpriteComponent.TextureRoot / node.AsResourcePath()).Texture;
            }

            RSI rsi;

            if (mapping.TryGetNode("sprite", out node))
            {
                var path = SpriteComponent.TextureRoot / node.AsResourcePath();
                try
                {
                    rsi = resc.GetResource<RSIResource>(path).RSI;
                }
                catch
                {
                    Logger.ErrorS("go.comp.icon", "Failed to load RSI '{0}' on prototype '{1}'", path, prototype.ID);
                    return resc.GetFallback<TextureResource>().Texture;
                }
            }
            else
            {
                return resc.GetFallback<TextureResource>().Texture;
            }

            if (mapping.TryGetNode("state", out node))
            {
                if (rsi.TryGetState(node.AsString(), out var state))
                {
                    return state.GetFrame(0, 0).icon;
                }
                else
                {
                    Logger.ErrorS("go.comp.icon", "State '{0}' does not exist on RSI. Prototype: '{1}'", node.AsString(), prototype.ID);
                    return resc.GetFallback<TextureResource>().Texture;
                }
            }
            Logger.ErrorS("go.comp.icon", "No state specified prototype '{0}'", prototype.ID);
            return resc.GetFallback<TextureResource>().Texture;
        }

        public static IDirectionalTextureProvider GetPrototypeIcon(EntityPrototype prototype)
        {
            if (!prototype.Components.TryGetValue("Icon", out var mapping))
            {
                return IoCManager.Resolve<IResourceCache>().GetFallback<TextureResource>().Texture;
            }

            return TextureForConfig(mapping, prototype);
        }
    }
}
