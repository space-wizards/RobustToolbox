using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Serialization;
using SS14.Shared.Utility;

namespace SS14.Client.GameObjects
{
    public class IconComponent : Component
    {
        public override string Name => "Icon";
        public IDirectionalTextureProvider Icon => _icon;
        private IDirectionalTextureProvider _icon;

        [Dependency] private readonly IResourceCache _resourceCache;

        public const string LogCategory = "go.comp.icon";
        const string SerializationCache = "icon";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: Does this need writing?
            if (serializer.Reading)
            {
                _icon = TextureForConfig(serializer, _resourceCache);
            }
        }

        private static IDirectionalTextureProvider TextureForConfig(ObjectSerializer serializer, IResourceCache resourceCache)
        {
            DebugTools.Assert(serializer.Reading);

            if (serializer.TryGetCacheData<IDirectionalTextureProvider>(SerializationCache, out var dirTex))
            {
                return dirTex;
            }

            var tex = serializer.ReadDataField<string>("texture", null);
            if (!string.IsNullOrWhiteSpace(tex))
            {
                dirTex = resourceCache.GetResource<TextureResource>(SpriteComponent.TextureRoot / tex).Texture;
                serializer.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            RSI rsi;

            var rsiPath = serializer.ReadDataField<string>("sprite", null);

            if (string.IsNullOrWhiteSpace(rsiPath))
            {
                dirTex = resourceCache.GetFallback<TextureResource>().Texture;
                serializer.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            var path = SpriteComponent.TextureRoot / rsiPath;

            try
            {
                rsi = resourceCache.GetResource<RSIResource>(path).RSI;
            }
            catch
            {
                dirTex = resourceCache.GetFallback<TextureResource>().Texture;
                serializer.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            var stateId = serializer.ReadDataField<string>("state", null);
            if (string.IsNullOrWhiteSpace(stateId))
            {
                Logger.ErrorS(LogCategory, "No state specified.");
                dirTex = resourceCache.GetFallback<TextureResource>().Texture;
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
                return resourceCache.GetFallback<TextureResource>().Texture;
            }
        }

        public static IDirectionalTextureProvider GetPrototypeIcon(EntityPrototype prototype, IResourceCache resourceCache)
        {
            if (!prototype.Components.TryGetValue("Icon", out var mapping))
            {
                return resourceCache.GetFallback<TextureResource>().Texture;
            }
            return TextureForConfig(YamlObjectSerializer.NewReader(mapping), resourceCache);
        }
    }
}
