using Robust.Shared.Graphics.RSI;

namespace Robust.Client.Graphics
{
    public interface IRsiStateLike : IDirectionalTextureProvider
    {
        RsiDirectionType RsiDirections { get; }
        bool IsAnimated { get; }
        int AnimationFrameCount { get; }

        float GetDelay(int frame);
        Texture GetFrame(RsiDirection dir, int frame);
    }
}
