using System;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.IoC;
using SS14.Shared.Utility;

namespace SS14.Client.Utility
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
