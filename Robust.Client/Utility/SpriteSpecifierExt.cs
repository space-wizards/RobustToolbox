using System;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Client.Utility
{
    public static class SpriteSpecifierExt
    {
        public static Texture Frame0(this SpriteSpecifier specifier)
        {
            var resc = IoCManager.Resolve<IResourceCache>();
            switch (specifier)
            {
                case SpriteSpecifier.Texture tex:
                    return resc.GetResource<TextureResource>(SpriteComponent.TextureRoot / tex.TexturePath);

                case SpriteSpecifier.Rsi rsi:
                    if (resc.TryGetResource<RSIResource>(SpriteComponent.TextureRoot / rsi.RsiPath, out var theRsi))
                    {
                        if (theRsi.RSI.TryGetState(rsi.RsiState, out var state))
                        {
                            return state.Frame0;
                        }
                    }
                    Logger.Error("Failed to load RSI {0}", rsi.RsiPath);
                    return resc.GetFallback<TextureResource>();

                default:
                    throw new NotImplementedException();
            }
        }

        public static IDirectionalTextureProvider DirFrame0(this SpriteSpecifier specifier)
        {
            var resc = IoCManager.Resolve<IResourceCache>();
            switch (specifier)
            {
                case SpriteSpecifier.Texture tex:
                    return (Texture)resc.GetResource<TextureResource>(SpriteComponent.TextureRoot / tex.TexturePath);

                case SpriteSpecifier.Rsi rsi:
                    if (resc.TryGetResource<RSIResource>(SpriteComponent.TextureRoot / rsi.RsiPath, out var theRsi))
                    {
                        if (theRsi.RSI.TryGetState(rsi.RsiState, out var state))
                        {
                            return state;
                        }
                    }
                    return (Texture)resc.GetFallback<TextureResource>();

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
