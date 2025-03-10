using System;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Robust.Client.Utility
{
    /// <summary>
    ///     Helper methods for resolving <see cref="SpriteSpecifier"/>s.
    /// </summary>
    public static class SpriteSpecifierExt
    {
        [Obsolete("Use SpriteSystem.GetTexture() instead")]
        public static Texture GetTexture(this SpriteSpecifier.Texture texSpecifier, IResourceCache cache)
        {
            return cache
                .GetResource<TextureResource>(SpriteSpecifierSerializer.TextureRoot / texSpecifier.TexturePath)
                .Texture;
        }

        [Obsolete("Use SpriteSystem.GetState() instead")]
        public static RSI.State GetState(this SpriteSpecifier.Rsi rsiSpecifier, IResourceCache cache)
        {
            if (!cache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / rsiSpecifier.RsiPath, out var theRsi))
            {
                var sys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();
                Logger.Error("SpriteSpecifier failed to load RSI {0}", rsiSpecifier.RsiPath);
                return sys.GetFallbackState();
            }

            if (theRsi.RSI.TryGetState(rsiSpecifier.RsiState, out var state))
            {
                return state;
            }

            Logger.Error($"SpriteSpecifier has invalid RSI state '{rsiSpecifier.RsiState}' for RSI: {rsiSpecifier.RsiPath}");
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>().GetFallbackState();
        }

        [Obsolete("Use SpriteSystem.Frame0() instead")]
        public static Texture Frame0(this SpriteSpecifier specifier)
        {
            return specifier.RsiStateLike().Default;
        }

        [Obsolete("Use SpriteSystem.RsiStateLike() instead")]
        public static IDirectionalTextureProvider DirFrame0(this SpriteSpecifier specifier)
        {
            return specifier.RsiStateLike();
        }

        [Obsolete("Use SpriteSystem.RsiStateLike() instead")]
        public static IRsiStateLike RsiStateLike(this SpriteSpecifier specifier)
        {
            var resC = IoCManager.Resolve<IResourceCache>();
            switch (specifier)
            {
                case SpriteSpecifier.Texture tex:
                    return tex.GetTexture(resC);

                case SpriteSpecifier.Rsi rsi:
                    return rsi.GetState(resC);

                case SpriteSpecifier.EntityPrototype prototypeIcon:
                    var protMgr = IoCManager.Resolve<IPrototypeManager>();
                    var sys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();
                    if (!protMgr.TryIndex<EntityPrototype>(prototypeIcon.EntityPrototypeId, out var prototype))
                    {
                        Logger.Error("Failed to load PrototypeIcon {0}", prototypeIcon.EntityPrototypeId);
                        return sys.GetFallbackState();
                    }

                    return SpriteComponent.GetPrototypeIcon(prototype, resC);

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
