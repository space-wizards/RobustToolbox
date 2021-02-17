using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Prototypes.DataClasses;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
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

        //TODO Paul: fix dis, also dont forget about exposedata to set the icon
        //TODO actually, do we even need this now?
        private static IRsiStateLike TextureForConfig(DataClass compData, IResourceCache resourceCache)
        {
            /*IDirectionalTextureProvider dirTex;

            //var tex = compData.ReadDataField<string?>("texture", null);
            if (serializer.TryGetCacheData<IRsiStateLike>(SerializationCache, out var dirTex))
            {
                dirTex = resourceCache.GetResource<TextureResource>(SpriteComponent.TextureRoot / (string)texObj).Texture;
                //todo compData.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            RSI rsi;

            if (compData.TryGetValue("sprite", out var rsiPathObj) && string.IsNullOrWhiteSpace((string?)rsiPathObj))
            {
                dirTex = resourceCache.GetFallback<TextureResource>().Texture;
                //todo compData.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            var path = SpriteComponent.TextureRoot / (string)rsiPathObj!;

            try
            {
                rsi = resourceCache.GetResource<RSIResource>(path).RSI;
            }
            catch
            {
                dirTex = resourceCache.GetFallback<TextureResource>().Texture;
                //todo compData.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            if (compData.TryGetValue("state", out var stateObj) && string.IsNullOrWhiteSpace((string?)stateObj))
            {
                Logger.ErrorS(LogCategory, "No state specified.");
                dirTex = resourceCache.GetFallback<TextureResource>().Texture;
                //todo compData.SetCacheData(SerializationCache, dirTex);
                return dirTex;
            }

            if (rsi.TryGetState((string)stateObj!, out var state))
            {
                //todo compData.SetCacheData(SerializationCache, state);
                return state;
            }
            else
            {
                Logger.ErrorS(LogCategory, "State '{0}' does not exist on RSI.", (string)stateObj!);
                return resourceCache.GetFallback<TextureResource>().Texture;
            }*/
            //TODO Paul: LATER
            return resourceCache.GetFallback<TextureResource>().Texture;
        }

        public static IRsiStateLike? GetPrototypeIcon(EntityPrototype prototype, IResourceCache resourceCache)
        {
            if (!prototype.Components.TryGetValue("Icon", out var compData))
            {
                return null;
            }
            return TextureForConfig(compData, resourceCache);
        }
    }
}
