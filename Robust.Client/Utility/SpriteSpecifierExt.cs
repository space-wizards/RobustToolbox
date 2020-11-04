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
                    return resc.GetResource<TextureResource>(SpriteComponent.TextureRoot / tex.TexturePath).Texture;

                case SpriteSpecifier.Rsi rsi:
                    if (resc.TryGetResource<RSIResource>(SpriteComponent.TextureRoot / rsi.RsiPath, out var theRsi))
                    {
                        if (theRsi.RSI.TryGetState(rsi.RsiState, out var state))
                        {
                            return state.Frame0;
                        }
                    }
                    Logger.Error("Failed to load RSI {0}", rsi.RsiPath);
                    return resc.GetFallback<TextureResource>().Texture;

                default:
                    throw new NotImplementedException();
            }
        }

        public static float[] FrameDelays(this SpriteSpecifier specifier) {
            var resc = IoCManager.Resolve<IResourceCache>();
            switch (specifier) {
                case SpriteSpecifier.Rsi rsi:
                    if (resc.TryGetResource<RSIResource>(SpriteComponent.TextureRoot / rsi.RsiPath, out var theRsi)) {
                        if (theRsi.RSI.TryGetState(rsi.RsiState, out var state)) {
                            return state.Delays;
                        }
                    }
                    Logger.Error("Failed to load RSI {0}", rsi.RsiPath);
                    return new float[0];

                default:
                    throw new NotImplementedException();
            }
        }

        public static Texture[] FrameArr(this SpriteSpecifier specifier) {
            var resc = IoCManager.Resolve<IResourceCache>();
            switch (specifier) {
                case SpriteSpecifier.Rsi rsi:
                    if (resc.TryGetResource<RSIResource>(SpriteComponent.TextureRoot / rsi.RsiPath, out var theRsi)) {
                        if (theRsi.RSI.TryGetState(rsi.RsiState, out var state)) {
                            return state.Icons[0];
                        }
                    }
                    Logger.Error("Failed to load RSI {0}", rsi.RsiPath);
                    return new Texture[0];

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
                    return resc.GetResource<TextureResource>(SpriteComponent.TextureRoot / tex.TexturePath).Texture;

                case SpriteSpecifier.Rsi rsi:
                    if (resc.TryGetResource<RSIResource>(SpriteComponent.TextureRoot / rsi.RsiPath, out var theRsi))
                    {
                        if (theRsi.RSI.TryGetState(rsi.RsiState, out var state))
                        {
                            return state;
                        }
                    }
                    return resc.GetFallback<TextureResource>().Texture;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
