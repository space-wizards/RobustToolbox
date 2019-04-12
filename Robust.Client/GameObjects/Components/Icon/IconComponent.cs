using Robust.Client.GameObjects.Components.Renderable;
using Robust.Client.Graphics;
using Robust.Client.Graphics.RSI;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects.Components.Icon
{
    public class IconComponent : Component
    {
        public override string Name => "Icon";
        public IDirectionalTextureProvider Icon => _icon;
        private IDirectionalTextureProvider _icon;

        public const string LogCategory = "go.comp.icon";
        const string SerializationCache = "icon";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: Does this need writing?
            if (serializer.Reading)
            {
                _icon = TextureForConfig(serializer);
            }
        }

        private static IDirectionalTextureProvider TextureForConfig(ObjectSerializer serializer)
        {
            var resc = IoCManager.Resolve<IResourceCache>();
            DebugTools.Assert(serializer.Reading);

            if (serializer.TryGetCacheData<IDirectionalTextureProvider>(SerializationCache, out var dirTex))
            {
                return dirTex;
            }

            var tex = serializer.ReadDataField<string>("texture", null);
            if (!string.IsNullOrWhiteSpace(tex))
            {
                dirTex = resc.GetResource<TextureResource>(SpriteComponent.TextureRoot / tex).Texture;
                serializer.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            RSI rsi;

            var rsiPath = serializer.ReadDataField<string>("sprite", null);

            if (string.IsNullOrWhiteSpace(rsiPath))
            {
                dirTex = resc.GetFallback<TextureResource>().Texture;
                serializer.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            var path = SpriteComponent.TextureRoot / rsiPath;

            try
            {
                rsi = resc.GetResource<RSIResource>(path).RSI;
            }
            catch
            {
                dirTex = resc.GetFallback<TextureResource>().Texture;
                serializer.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            var stateId = serializer.ReadDataField<string>("state", null);
            if (string.IsNullOrWhiteSpace(stateId))
            {
                Logger.ErrorS(LogCategory, "No state specified.");
                dirTex = resc.GetFallback<TextureResource>().Texture;
                serializer.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            if (rsi.TryGetState(stateId, out var state))
            {
                serializer.SetCacheData(SerializationCache, state);
                return state;
            }
            else
            {
                Logger.ErrorS(LogCategory, "State '{0}' does not exist on RSI.", stateId);
                return resc.GetFallback<TextureResource>().Texture;
            }
        }

        public static IDirectionalTextureProvider GetPrototypeIcon(EntityPrototype prototype)
        {
            if (!prototype.Components.TryGetValue("Icon", out var mapping))
            {
                return IoCManager.Resolve<IResourceCache>().GetFallback<TextureResource>().Texture;
            }
            return TextureForConfig(YamlObjectSerializer.NewReader(mapping));
        }
    }
}
