using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    public class IconComponent : Component
    {
        public override string Name => "Icon";
        public IDirectionalTextureProvider? Icon { get; private set; }

        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public const string LogCategory = "go.comp.icon";
        const string SerializationCache = "icon";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: Does this need writing?
            if (serializer.Reading)
            {
                Icon = TextureForConfig(serializer, _resourceCache);
            }
        }

        private static IRsiStateLike TextureForConfig(ObjectSerializer serializer, IResourceCache resourceCache)
        {
            DebugTools.Assert(serializer.Reading);

            if (serializer.TryGetCacheData<IRsiStateLike>(SerializationCache, out var dirTex))
            {
                return dirTex;
            }

            var tex = serializer.ReadDataField<string?>("texture", null);
            if (!string.IsNullOrWhiteSpace(tex))
            {
                dirTex = resourceCache.GetResource<TextureResource>(SpriteComponent.TextureRoot / tex).Texture;
                serializer.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            RSI rsi;

            var rsiPath = serializer.ReadDataField<string?>("sprite", null);

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

            var stateId = serializer.ReadDataField<string?>("state", null);
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

        public static IRsiStateLike? GetPrototypeIcon(EntityPrototype prototype, IResourceCache resourceCache)
        {
            if (!prototype.Components.TryGetValue("Icon", out var mapping))
            {
                return null;
            }
            return TextureForConfig(YamlObjectSerializer.NewReader(mapping), resourceCache);
        }
    }
}
